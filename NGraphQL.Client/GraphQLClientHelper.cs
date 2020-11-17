using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace NGraphQL.Client {
  using IDict = IDictionary<string, object>;

  public static class GraphQLClientHelper {

    public static void EnsureNoErrors(this ServerResponse response) {
      if (response.Errors != null && response.Errors.Count > 0)
        throw new Exception("GraphQL request failed.");
    }

    public static string GetPayloadJson(this ClientRequest request) {
      var payload = request.GetPayload();
      var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
      return json; 
    }
    
    public static IDict GetPayload(this ClientRequest request) {
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

  }
}
