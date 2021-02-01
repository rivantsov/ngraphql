using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGraphQL.Client.Serialization;

namespace NGraphQL.Client {
  public partial class GraphQLClient {

    private async Task SendAsync(ClientRequest request, ServerResponse response) {
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
      var respMessage = await _client.SendAsync(reqMessage, request.CompletionOption, request.CancellationToken);
      respMessage.EnsureSuccessStatusCode();
      await ReadServerResponseAsync(response, respMessage);
    }

    private async Task ReadServerResponseAsync(ServerResponse response, HttpResponseMessage respMessage) {
      response.BodyJson = await respMessage.Content.ReadAsStringAsync();
      response.TopFields = JsonConvert.DeserializeObject<IDictionary<string, JToken>>(response.BodyJson);
      if (response.TopFields.TryGetValue("errors", out var errs) && errs != null) {
        response.Errors = errs.ToObject<IList<GraphQLError>>(); //convert to strongly-typed objects
      }
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      var payloadDict = BuildPayload(request.CoreRequest);
      // use this settings object to ensure camel-case names in objects (varirable values)
      request.Body = JsonConvert.SerializeObject(payloadDict, ClientSerializers.TypedJsonSerializerSettings);
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
      var varsJson = JsonConvert.SerializeObject(req.Variables, ClientSerializers.UrlJsonSettings);
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
