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
  using IDict = IDictionary<string, object>;

  public class GraphQLClient {
    public const string MediaTypeJson = "application/json";
    public const string MediaTypeText = "application/text";

    public readonly string ServiceUrl;
    public readonly Uri ServiceUri;
    public event EventHandler<RequestStartingEventArgs> RequestStarting;
    public event EventHandler<RequestCompletedEventArgs> RequestCompleted;


    HttpClient _client;
    JsonSerializerSettings _serializerSettings;

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

    public Task<ServerResponse> PostAsync(string query, IDict variables = null, string operationName = null, 
                                         CancellationToken cancellationToken = default, bool throwOnError = false) {
      var request = new ClientRequest() {
        RequestType = RequestType.Post, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken, ThrowOnError = throwOnError
      };
      return SendAsync(request);
    }

    public Task<ServerResponse> GetAsync(string query, IDict variables = null, string operationName = null, 
                          CancellationToken cancellationToken = default, bool throwOnError = false) {
      var request = new ClientRequest() {
        RequestType = RequestType.Get, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken, ThrowOnError = throwOnError
      };
      return SendAsync(request);
    }

    public async Task<string> GetSchemaDocument(string url = "/schema", CancellationToken cancellationToken = default) {
      var reqMsg = new HttpRequestMessage(HttpMethod.Get, ServiceUrl + url);
      reqMsg.Headers.Add("accept", MediaTypeText);
      var respMsg = await _client.SendAsync(reqMsg, cancellationToken);
      respMsg.EnsureSuccessStatusCode();
      var doc = await respMsg.Content.ReadAsStringAsync();
      return doc; 
    }

    public async Task<ServerResponse> SendAsync(ClientRequest request) {
      var start = GetTimestamp();
      var response = new ServerResponse() { Request = request };
      try {
        RequestStarting?.Invoke(this, new RequestStartingEventArgs(request));
        await SendAsync(request, response);
        response.TimeMs = GetTimeSince(start);
        RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(response));
      } catch (Exception ex) {
        response.Exception = ex;
        RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(response));
        if (request.ThrowOnError && response.Exception != null) {
          if (response.Exception == ex)
            throw;
          else
            throw response.Exception; //throw new exception
        }
      } 
      return response;
    }

    private async Task SendAsync(ClientRequest request, ServerResponse response) {
      var reqMessage = new HttpRequestMessage();
      switch(request.RequestType) {

        case RequestType.Post:
          reqMessage.RequestUri = ServiceUri;
          reqMessage.Method = HttpMethod.Post; 
          reqMessage.Content = BuildPostMessageContent(request);
          break;
        
        case RequestType.Get:
          reqMessage.Method = HttpMethod.Get;
          request.GetUrlQuery = BuildGetMessageUrlQuery(request);
          reqMessage.RequestUri = new Uri(ServiceUrl + "?" + request.GetUrlQuery);
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

      await ReadServerResponseAsync(response, respMessage);  
    } 

    private async Task ReadServerResponseAsync(ServerResponse response, HttpResponseMessage respMessage) {
      var json = await respMessage.Content.ReadAsStringAsync();
      response.Payload = JsonConvert.DeserializeObject<ExpandoObject>(json, _serializerSettings);
      if (response.Payload.TryGetValue("errors", out var errorsObj) && errorsObj is IList list && list.Count > 0) {
        response.Errors = ConvertErrors(errorsObj); //convert to strongly-typed objects
      }
      if (response.Payload.TryGetValue("data", out var data))
        response.Data = data;
    }

    // convert to strongly-typed Error objects
    private IList<RequestError> ConvertErrors(object errors) {
      var json = JsonConvert.SerializeObject(errors);
      var errList = JsonConvert.DeserializeObject<IList<RequestError>>(json);
      return errList; 
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      request.PostPayload = request.GetPayload();
      var json = JsonConvert.SerializeObject(request.PostPayload, Formatting.Indented);
      var content = new StringContent(json, Encoding.UTF8, MediaTypeJson);
      return content; 
    }

    // see https://graphql.org/learn/serving-over-http/#get-request
    private string BuildGetMessageUrlQuery(ClientRequest request) {
      var urlQry = "query=" + Uri.EscapeUriString(request.Query);
      if (!string.IsNullOrWhiteSpace(request.OperationName))
        urlQry += "&operationName=" + Uri.EscapeUriString(request.OperationName);
      if (request.Variables == null || request.Variables.Count == 0)
        return urlQry;
      // serializer vars as json, and add to URL qry
      // do not use settings here, we don't need fancy settings here from body serialization process
      var varsJson = JsonConvert.SerializeObject(request.Variables, Formatting.None);
      urlQry += "&variables=" + Uri.EscapeUriString(varsJson);
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
