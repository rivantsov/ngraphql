using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NGraphQL.Client {
  public partial class GraphQLClient {

    private async Task SendAsync(ClientRequest request, ServerResponse response) {
      var reqMessage = new HttpRequestMessage();
      switch (request.Method) {

        case RequestMethod.Post:
          //reqMessage.RequestUri = _serviceUri;
          reqMessage.Method = HttpMethod.Post;
          reqMessage.Content = BuildPostMessageContent(request);
          break;

        case RequestMethod.Get:
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
      var json = await respMessage.Content.ReadAsStringAsync();
      response.Payload = JsonConvert.DeserializeObject<ExpandoObject>(json, _expandoObjectsJsonSettings);
      if (response.Payload.TryGetValue("errors", out var errorsObj) && errorsObj is IList list && list.Count > 0) {
        response.Errors = ConvertErrors(errorsObj); //convert to strongly-typed objects
      }
      if (response.Payload.TryGetValue("data", out var data))
        response.Data = data;
    }

    // convert to strongly-typed Error objects
    private IList<RequestError> ConvertErrors(object errors) {
      var json = JsonConvert.SerializeObject(errors, _typedJsonSettings);
      var errList = JsonConvert.DeserializeObject<IList<RequestError>>(json, _typedJsonSettings);
      return errList;
    }

    private HttpContent BuildPostMessageContent(ClientRequest request) {
      request.PostPayload = BuildPayload(request);
      var json = JsonConvert.SerializeObject(request.PostPayload, _typedJsonSettings);
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
      var varsJson = JsonConvert.SerializeObject(request.Variables, _urlJsonSettings);
      urlQry += "&variables=" + Uri.EscapeUriString(varsJson);
      return urlQry;
    }

    private IDictionary<string, object> BuildPayload(ClientRequest request) {
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

    private static int GetTimeSince(long start) {
      var now = Stopwatch.GetTimestamp();
      var timeMs = (now - start) * 1000 / Stopwatch.Frequency;
      return (int)timeMs;
    }
  }

}
