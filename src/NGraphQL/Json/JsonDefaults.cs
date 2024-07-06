using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NGraphQL.Json {

  public static class JsonDefaults {
    public static readonly JsonSerializerOptions JsonOptions;
    // for deserializing types with 'object' type field - to leave it as JsonElement, to deserialize later
    public static readonly JsonSerializerOptions JsonOptionsPartial;
    public static readonly JsonSerializerOptions JsonUrlOptions;

    static JsonDefaults() {
      var baseOptions = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        WriteIndented = true
      };
      baseOptions.Converters.Add(new Json.EnumConverterFactory());

      // Slim options - for deserializing without converting untyped objects, they remain JsonElement
      // we use it in Subscriptions
      JsonOptionsPartial = new JsonSerializerOptions(baseOptions);

      // general options
      JsonOptions = new JsonSerializerOptions(baseOptions); 
      JsonOptions.Converters.Add(new DefaultObjectConverter()); //for converting untyped, including values inside dictionaries

      // Url options is a copy with WriteIndented = false
      JsonUrlOptions = new JsonSerializerOptions(JsonOptions);
      JsonUrlOptions.WriteIndented = false;
    }

  }
}
