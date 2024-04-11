using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NGraphQL.Client.Types {

  public class DeserializedGraphQLResponse {
    [JsonPropertyName("data")]
    public JsonElement Data;

    [JsonPropertyName("errors")]
    public IList<GraphQLError> Errors; 
  }
}
