using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NGraphQL.Client {
  public partial class GraphQLClient {

    private async Task SendAsync(ClientRequest request, GraphQLResult response) {
      var reqMessage = new HttpRequestMessage();
      switch (request.HttpMethod) {

        case "POST":
          //reqMessage.RequestUri = _serviceUri;
          reqMessage.Method = HttpMethod.Post;
          reqMessage.Content = BuildPostMessageContent(request);
          break;

        case "GET":
          reqMessage.Method = HttpMethod.Get;
          request.UrlQueryPartForGet = BuildGetMessageUrlQuery(request);
          reqMessage.RequestUri = new Uri(_endpointUrl + "?" + request.UrlQueryPartForGet);
          break;
      }
      // Headers - copy default headers and custom headers
      var reqHeaders = reqMessage.Headers;
      reqHeaders.Add("accept", MediaTypeJson);
      if (request.Headers != null)
        foreach (var de in request.Headers)
          reqHeaders.Add(de.Key, de.Value);

      // actually execute
      var respMessage = await _client.SendAsync(reqMessage, HttpCompletionOption.ResponseContentRead, request.CancellationToken);
      respMessage.EnsureSuccessStatusCode();
      await ReadServerResponseAsync(response, respMessage);
    }

    private async Task ReadServerResponseAsync(GraphQLResult result, HttpResponseMessage respMessage) {
      result.ResponseJson = await respMessage.Content.ReadAsStringAsync();
      result.ResponseBody = JsonSerializer.Deserialize<GraphQLResponseBody>(result.ResponseJson);
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      var payloadDict = BuildPayload(request.CoreRequest);
      request.Body = JsonSerializer.Serialize(payloadDict, _jsonOptions);
      var content = new StringContent(request.Body, Encoding.UTF8, MediaTypeJson);
      return content;
    }

    // see https://graphql.org/learn/serving-over-http/#get-request
    private string BuildGetMessageUrlQuery(ClientRequest request) {
      var req = request.CoreRequest;
      var urlQry = "query=" + Uri.EscapeUriString(req.Query);
      if (!string.IsNullOrWhiteSpace(req.OperationName))
        urlQry += "&operationName=" + Uri.EscapeUriString(req.OperationName);
      if (req.Variables == null || req.Variables.Count == 0)
        return urlQry;
      // serializer vars as json, and add to URL qry
      // do not use settings here, we don't need fancy settings here from body serialization process
      var varsJson = JsonSerializer.Serialize(req.Variables, _jsonUrlOptions);
      urlQry += "&variables=" + Uri.EscapeUriString(varsJson);
      return urlQry;
    }

    private IDictionary<string, object> BuildPayload(GraphQLRequest request) {
      var dict = new Dictionary<string, object>();
      dict["query"] = request.Query;
      var vars = request.Variables;
      if (vars != null && vars.Count > 0) {
        dict["variables"] = vars;
      }
      if (!string.IsNullOrWhiteSpace(request.OperationName))
        dict["operationName"] = request.OperationName;
      return dict;
    }

    private static long GetTimestamp() {
      return Stopwatch.GetTimestamp();
    }

    private static double GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var timeMs = (now - start) * 1000.0 / Stopwatch.Frequency;
      return timeMs;
    }
  }

}
