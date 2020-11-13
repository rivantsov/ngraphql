using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NGraphQL.Client {
  using TDict = Dictionary<string, object>;

  public class GraphQLClient {
    public const string MediaTypeJson = "application/json";
    JsonSerializerSettings _serializerSettings = new JsonSerializerSettings();

    public readonly string ServiceUrl; 
    HttpClient _client;

    public GraphQLClient(string serviceUrl) {
      ServiceUrl = serviceUrl;
      _client = new HttpClient();
    }

    #region Headers
    public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null;
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }
   
    #endregion

    public async Task<dynamic> PostAsync(string query, TDict variables = null, CancellationToken cancellationToken = default) {
      var request = new ClientRequest() {
        RequestType = RequestType.Post, Query = query, Variables = variables,
        CancellationToken = cancellationToken
      };
      var resp = await SendAsync(request);
      return resp; 
    }

    private async Task<ServerResponse> SendAsync(ClientRequest request) {
      var start = GetTimestamp();
      var reqMessage = new HttpRequestMessage();
      reqMessage.RequestUri = new Uri(ServiceUrl);
      switch(request.RequestType) {
        case RequestType.Post:
          reqMessage.Method = HttpMethod.Post; 
          reqMessage.Content = BuildPostMessageContent(request);
          break;
        case RequestType.Get:
          reqMessage.Method = HttpMethod.Get;
          reqMessage.Content = BuildGetMessageContent(request);
          break;
      }
      // Headers - copy default headers and custom headers
      var headers = reqMessage.Headers;
      headers.Add("accept", MediaTypeJson);
      foreach (var kv in this.DefaultRequestHeaders)
        headers.Add(kv.Key, kv.Value);
      if (request.Headers != null)
        foreach (var de in request.Headers)
          headers.Add(de.Key, de.Value);
      
      // actually execute
      var respMessage = await _client.SendAsync(reqMessage, request.CompletionOption, request.CancellationToken);
      respMessage.EnsureSuccessStatusCode();

      var resp = await ReadServerResponseAsync(respMessage);  
      resp.TimeMs = GetTimeSince(start);
      return resp; 
    } 

    private async Task<ServerResponse> ReadServerResponseAsync(HttpResponseMessage respMessage) {
      var json = await respMessage.Content.ReadAsStringAsync();
      var bodyDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);
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
      if (request.Variables != null && request.Variables.Count > 0) {
        bodyDict["variables"] = request.Variables;
      }
      var strBody = JsonConvert.SerializeObject(bodyDict, _serializerSettings);
      var content = new StringContent(strBody, Encoding.UTF8, MediaTypeJson);
      return content; 
    }

    private HttpContent BuildGetMessageContent(ClientRequest request) {
      throw new NotImplementedException();
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
