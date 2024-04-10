using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NGraphQL.Client {

  public class GraphQLResponseBody {
    [JsonPropertyName("data")]
    public JsonElement Data;
    [JsonPropertyName("errors")]
    public IList<GraphQLError> Errors;
  }
}
