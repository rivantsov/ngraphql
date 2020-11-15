using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace NGraphQL.Client {
  using TDict = Dictionary<string, object>;
  using IDict = IDictionary<string, object>;

  public class GraphQLClient {
    public const string MediaTypeJson = "application/json";
    JsonSerializerSettings _serializerSettings;

    public readonly string ServiceUrl;
    public readonly Uri ServiceUri;
    HttpClient _client;

    public GraphQLClient(string serviceUrl) {
      ServiceUrl = serviceUrl;
      ServiceUri = new Uri(ServiceUrl);
      _serializerSettings = new JsonSerializerSettings();
      _serializerSettings.Converters.Add(new ExpandoObjectConverter());
      _client = new HttpClient();
      
    }

    #region Headers
    public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null;
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }
   
    #endregion

    public async Task<dynamic> PostAsync(string query, TDict variables = null, string operationName = null, 
                                         CancellationToken cancellationToken = default) {
      var request = new ClientRequest() {
        RequestType = RequestType.Post, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken
      };
      var resp = await SendAsync(request);
      return resp; 
    }

    public async Task<dynamic> GetAsync(string query, TDict variables = null, string operationName = null, 
                                        CancellationToken cancellationToken = default) {
      var request = new ClientRequest() {
        RequestType = RequestType.Get, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken
      };
      var resp = await SendAsync(request);
      return resp;
    }

    public async Task<ServerResponse> SendAsync(ClientRequest request) {
      var start = GetTimestamp();
      var reqMessage = new HttpRequestMessage();
      switch(request.RequestType) {
        case RequestType.Post:
          reqMessage.RequestUri = ServiceUri;
          reqMessage.Method = HttpMethod.Post; 
          reqMessage.Content = BuildPostMessageContent(request);
          break;
        case RequestType.Get:
          reqMessage.Method = HttpMethod.Get;
          var urlQuery = BuildGetMessageUrlQuery(request);
          reqMessage.RequestUri = new Uri(ServiceUrl + "?" + urlQuery);
          break;
      }
      // Headers - copy default headers and custom headers
      var reqHeaders = reqMessage.Headers;
      reqHeaders.Add("accept", MediaTypeJson);
      foreach (var kv in this.DefaultRequestHeaders)
        reqHeaders.Add(kv.Key, kv.Value);
      if (request.Headers != null)
        foreach (var de in request.Headers)
          reqHeaders.Add(de.Key, de.Value);
      
      // actually execute
      var respMessage = await _client.SendAsync(reqMessage, request.CompletionOption, request.CancellationToken);
      respMessage.EnsureSuccessStatusCode();

      var resp = await ReadServerResponseAsync(respMessage);  
      resp.TimeMs = GetTimeSince(start);
      return resp; 
    } 

    private async Task<ServerResponse> ReadServerResponseAsync(HttpResponseMessage respMessage) {
      var json = await respMessage.Content.ReadAsStringAsync();
      IDictionary<string, object> bodyDict = JsonConvert.DeserializeObject<ExpandoObject>(json, _serializerSettings);
      var resp = new ServerResponse();
      if (bodyDict.TryGetValue("errors", out var errorsObj) && errorsObj is JObject errJObj) {
        resp.Errors = errJObj.ToObject<IList<ServerError>>();
      }
      if (bodyDict.TryGetValue("data", out var data))
        resp.Data = data;
      return resp; 
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      var bodyDict = new Dictionary<string, object>();
      bodyDict["query"] = request.Query;
      var vars = request.Variables;
      if (vars != null && vars.Count > 0) {
        bodyDict["variables"] = vars;
      }
      if (!string.IsNullOrWhiteSpace(request.OperationName))
        bodyDict["operationName"] = request.OperationName;
      var strBody = JsonConvert.SerializeObject(bodyDict, _serializerSettings);
      var content = new StringContent(strBody, Encoding.UTF8, MediaTypeJson);
      return content; 
    }

    // see https://graphql.org/learn/serving-over-http/#get-request
    private string BuildGetMessageUrlQuery(ClientRequest request) {
      var urlQry = "?query=" + Uri.EscapeUriString(request.Query);
      if (!string.IsNullOrWhiteSpace(request.OperationName))
        urlQry += "&operationName=" + Uri.EscapeUriString(request.OperationName);
      if (request.Variables == null || request.Variables.Count == 0)
        return urlQry;
      // serializer vars as json, and add to URL qry
      // do not use settings here, we don't need fancy settings here from body serialization process
      var varsJson = JsonConvert.SerializeObject(request.Variables, Formatting.None);
      urlQry += "&" + Uri.EscapeUriString(varsJson);
      return urlQry;       
    }

    private static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    private static int GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var timeMs = (now - start) * 1000 / Stopwatch.Frequency;
      return (int) timeMs;
    }
  }

}
