using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NGraphQL.Json {

  public static class JsonDefaults {
    public static readonly JsonSerializerOptions JsonOptions;
    public static readonly JsonSerializerOptions JsonUrlOptions;

    static JsonDefaults() {
      JsonOptions = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        WriteIndented = true
      };
      JsonOptions.Converters.Add(new Json.EnumConverterFactory());
      JsonOptions.Converters.Add(new ObjectAsPrimitiveConverter()); //for converting values inside dictionaries
      // Url options is a copy with WriteIndented = false
      JsonUrlOptions = new JsonSerializerOptions(JsonOptions);
      JsonUrlOptions.WriteIndented = false;
    }

  }
}
