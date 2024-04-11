using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NGraphQL.Client.Types;

namespace NGraphQL.Client {
  public partial class GraphQLClient {

    private async Task SendAsync(GraphQLResult result) {
      var request = result.Request;
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
      result.ResponseJson = await respMessage.Content.ReadAsStringAsync();
      result.ResponseBody = JsonSerializer.Deserialize<DeserializedGraphQLResponse>(result.ResponseJson, JsonOptions);
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      var reqDict = ConvertToSparseDictionary(request.Body);
      request.BodyJson = JsonSerializer.Serialize(reqDict, JsonOptions);
      var content = new StringContent(request.BodyJson, Encoding.UTF8, MediaTypeJson);
      return content;
    }

    // see https://graphql.org/learn/serving-over-http/#get-request
    private string BuildGetMessageUrlQuery(ClientRequest request) {
      var req = request.Body;
      var urlQry = "query=" + Uri.EscapeUriString(req.Query);
      if (!string.IsNullOrWhiteSpace(req.OperationName))
        urlQry += "&operationName=" + Uri.EscapeUriString(req.OperationName);
      if (req.Variables == null || req.Variables.Count == 0)
        return urlQry;
      // serializer vars as json, and add to URL qry
      // do not use settings here, we don't need fancy settings here from body serialization process
      var varsJson = JsonSerializer.Serialize(req.Variables, JsonUrlOptions);
      urlQry += "&variables=" + Uri.EscapeUriString(varsJson);
      return urlQry;
    }

    // we do not serialize request directly, but first convert it to dictionary, to make sure
    //  null values do not show up in Json.
    private IDictionary<string, object> ConvertToSparseDictionary(GraphQLRequest request) {
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
