using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NGraphQL.Client {
  public class GraphQLClient {
    public const string MediaTypeJson = "application/json";

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

    private async Task<ServerResponse> SendAsync(ClientRequest request) {
      var start = GetTimestamp(); 
      HttpRequestMessage reqMessage = null; 
      switch(request.RequestType) {
        case RequestType.Post:
          reqMessage = BuildPostMessage(request);
          break;
        case RequestType.Get:
          reqMessage = BuildGetMessage(request);
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
      
      var respMessage = await _client.SendAsync(reqMessage, request.CancellationToken);


      var timeMs = GetTimeSince(start); 
    } 

    private HttpRequestMessage BuildPostMessage(ClientRequest request) {
      return null; 
    }

    private HttpRequestMessage BuildGetMessage(ClientRequest request) {
      return null;
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
