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

  string _endpointUrl; 
  HttpClient _client;

  public GraphQLClient(string endpointUrl): this() {
    _endpointUrl = endpointUrl;
    _client = new HttpClient();
    _client.BaseAddress = new Uri(endpointUrl);
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
    } catch (Exception ex) {
      result.Exception = ex;   
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


}
