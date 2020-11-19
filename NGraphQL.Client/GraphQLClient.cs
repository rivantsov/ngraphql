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
    // Deserializer settings with ExpandoObjectConverter for deserializing data into dynamic object
    JsonSerializerSettings _expandoObjectsJsonSettings;
    // settings for regular strong-typed object, serializing body mostly
    JsonSerializerSettings _typedJsonSettings;
    // serializer for variables in URL (GET queries) - non-indented formatting
    JsonSerializerSettings _urlJsonSettings;

    public GraphQLClient(string endpointUrl) {
      _endpointUrl = endpointUrl;
      _client = new HttpClient();
      _client.BaseAddress = new Uri(endpointUrl);
      InitSerializerSettings(); 
    }

    public GraphQLClient(HttpClient httpClient) {
      _client = httpClient;
      _endpointUrl = httpClient.BaseAddress.ToString();
      InitSerializerSettings();
    }

    private void InitSerializerSettings() {
      _expandoObjectsJsonSettings = new JsonSerializerSettings();
      _expandoObjectsJsonSettings.Converters.Add(new ExpandoObjectConverter());
      _typedJsonSettings = new JsonSerializerSettings();
      _typedJsonSettings.Formatting = Formatting.Indented;
      _typedJsonSettings.ContractResolver = new DefaultContractResolver {
        NamingStrategy = new CamelCaseNamingStrategy()
      };
      _urlJsonSettings = new JsonSerializerSettings();
      _urlJsonSettings.Formatting = Formatting.None;
    }

    #region Headers
    public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

    public void AddAuthorizationHeader(string headerValue, string scheme = "Bearer") {
      DefaultRequestHeaders.Authorization = null;
      DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, headerValue);
    }

    #endregion

    public Task<ServerResponse> PostAsync(string query, IDict variables = null, string operationName = null,
                                         CancellationToken cancellationToken = default) {
      var request = new ClientRequest() {
        Method = RequestMethod.Post, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken
      };
      return SendAsync(request);
    }

    public Task<ServerResponse> GetAsync(string query, IDict variables = null, string operationName = null,
                          CancellationToken cancellationToken = default) {
      var request = new ClientRequest() {
        Method = RequestMethod.Get, Query = query, Variables = variables, OperationName = operationName,
        CancellationToken = cancellationToken
      };
      return SendAsync(request);
    }

    public async Task<string> GetSchemaDocument(string url = "/schema", CancellationToken cancellationToken = default) {
      var reqMsg = new HttpRequestMessage(HttpMethod.Get, url);
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
        if (response.Exception != null) {
          if (response.Exception == ex)
            throw;
          else
            throw response.Exception; //throw new exception
        }
      }
      return response;
    }

  }
}
