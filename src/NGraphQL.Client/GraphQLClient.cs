using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NGraphQL.Client.Types;
using NGraphQL.Json;
using NGraphQL.Subscriptions;

namespace NGraphQL.Client;
using IDict = IDictionary<string, object>;

public partial class GraphQLClient {
  public const string MediaTypeJson = "application/json";
  public const string MediaTypeText = "application/text";
  public JsonSerializerOptions JsonOptions;
  public JsonSerializerOptions JsonUrlOptions;

  public event EventHandler<RequestStartingEventArgs> RequestStarting;
  public event EventHandler<RequestCompletedEventArgs> RequestCompleted;
  public event EventHandler<SubscriptionMessageEventArgs> MessageReceived;
  public event EventHandler<ErrorMessageEventArgs> ErrorReceived;
  public event EventHandler<RequestErrorEventArgs> OnError;

  string _endpointUrl; 
  HttpClient _client;

  public GraphQLClient(string endpointUrl, bool enableSubscriptions = false): this() {
    _endpointUrl = endpointUrl;
    _client = new HttpClient();
    _client.BaseAddress = new Uri(endpointUrl);
    if (enableSubscriptions)
      InitSubscriptions(this._endpointUrl + "/subscriptions");
  }

  public GraphQLClient(HttpClient httpClient): this() {
    _client = httpClient;
    _endpointUrl = httpClient.BaseAddress.ToString();
  }

  private GraphQLClient() {
    JsonOptions = JsonDefaults.JsonOptions;
    JsonUrlOptions = JsonDefaults.JsonUrlOptions;
  }

  #region Headers
  public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

  public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
    DefaultRequestHeaders.Authorization = null;
    DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
  }
  public void ClearAuthorizationHeader() {
    DefaultRequestHeaders.Authorization = null; 
  }

  #endregion

  public Task<GraphQLResult> PostAsync(string query, IDict variables = null, string operationName = null,
                                       CancellationToken cancellationToken = default) {
    var request = new GraphQLRequest() { Query = query, Variables = variables, OperationName = operationName };
    var reqData = new ClientRequest() {
      HttpMethod = "POST", Body = request, CancellationToken = cancellationToken
    };
    return SendAsync(reqData);
  }

  public Task<GraphQLResult> GetAsync(string query, IDict variables = null, string operationName = null,
                        CancellationToken cancellationToken = default) {
    var request = new GraphQLRequest() { Query = query, Variables = variables, OperationName = operationName };
    var reqData = new ClientRequest() {
      HttpMethod = "GET", Body = request, CancellationToken = cancellationToken
    };
    return SendAsync(reqData);
  }

  public async Task<string> GetSchemaDocument(string url = "graphql/schema", CancellationToken cancellationToken = default) {
    var reqMsg = new HttpRequestMessage(HttpMethod.Get, url);
    reqMsg.Headers.Add("accept", MediaTypeText);
    var respMsg = await _client.SendAsync(reqMsg, cancellationToken);
    respMsg.EnsureSuccessStatusCode();
    var doc = await respMsg.Content.ReadAsStringAsync();
    return doc;
  }

  public async Task<GraphQLResult> SendAsync(ClientRequest request) {
    var start = GetTimestamp();
    var result = new GraphQLResult(request, JsonOptions);
    try {
      RequestStarting?.Invoke(this, new RequestStartingEventArgs(request));
      await SendAsync(result);
      result.DurationMs = GetTimeSince(start);
      RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(result));
      if (result.HasErrors())
        ReportResultErrors(result); 
    } catch (Exception ex) {
      result.Exception = ex;
      OnError?.Invoke(this, new RequestErrorEventArgs(ex, "Request: " + request.Body?.Query));
      RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(result));
      if (result.Exception != null) {
        if (result.Exception == ex)
          throw;
        else
          throw result.Exception; //throw new exception
      }
    }
    return result;
  }

  private void ReportResultErrors(GraphQLResult result) {
    if (!result.HasErrors())
      return;
    var exc = new GraphQLException("GraphQL operation failed, client received errors from the server.");
    var query = result.Request?.Body?.Query;
    var errors = result.GetErrorsAsText();
    var info = $@"Request: 
{query}
Errors: 
{errors}
";
    var args = new RequestErrorEventArgs(exc, info);
    OnError?.Invoke(this, args);
  }

}
