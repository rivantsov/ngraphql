using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NGraphQL.Json {

  public static class JsonDefaults {
    public static readonly JsonSerializerOptions JsonOptions;
    public static readonly JsonSerializerOptions JsonOptionsSlim;
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
      JsonOptionsSlim = new JsonSerializerOptions(baseOptions);

      // general options
      JsonOptions = new JsonSerializerOptions(baseOptions); 
      JsonOptions.Converters.Add(new DefaultObjectConverter()); //for converting untyped, including values inside dictionaries

      // Url options is a copy with WriteIndented = false
      JsonUrlOptions = new JsonSerializerOptions(JsonOptions);
      JsonUrlOptions.WriteIndented = false;
    }

  }
}
