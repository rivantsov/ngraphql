using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NGraphQL.Client {
  using IDict = IDictionary<string, object>;

  public partial class GraphQLClient {
    public const string MediaTypeJson = "application/json";
    public const string MediaTypeText = "application/text";

    public event EventHandler<RequestStartingEventArgs> RequestStarting;
    public event EventHandler<RequestCompletedEventArgs> RequestCompleted;

    string _endpointUrl; 
    HttpClient _client;

    public GraphQLClient(string endpointUrl) {
      _endpointUrl = endpointUrl;
      _client = new HttpClient();
      _client.BaseAddress = new Uri(endpointUrl);
    }

    public GraphQLClient(HttpClient httpClient) {
      _client = httpClient;
      _endpointUrl = httpClient.BaseAddress.ToString();
    }

    #region Headers
    public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null;
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }

    #endregion

    public Task<ResponseData> PostAsync(string query, IDict variables = null, string operationName = null,
                                         CancellationToken cancellationToken = default) {
      var request = new GraphQLRequest() { Query = query, Variables = variables, OperationName = operationName };
      var reqData = new RequestData() {
        HttpMethod = "POST", Request = request, CancellationToken = cancellationToken
      };
      return SendAsync(reqData);
    }

    public Task<ResponseData> GetAsync(string query, IDict variables = null, string operationName = null,
                          CancellationToken cancellationToken = default) {
      var request = new GraphQLRequest() { Query = query, Variables = variables, OperationName = operationName };
      var reqData = new RequestData() {
        HttpMethod = "GET", Request = request, CancellationToken = cancellationToken
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

    public async Task<ResponseData> SendAsync(RequestData requestData) {
      var start = GetTimestamp();
      var responseData = new ResponseData(requestData);
      try {
        RequestStarting?.Invoke(this, new RequestStartingEventArgs(requestData));
        await SendAsync(requestData, responseData);
        responseData.DurationMs = GetTimeSince(start);
        RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(responseData));
      } catch (Exception ex) {
        responseData.Exception = ex;
        RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(responseData));
        if (responseData.Exception != null) {
          if (responseData.Exception == ex)
            throw;
          else
            throw responseData.Exception; //throw new exception
        }
      }
      return responseData;
    }

  }
}
