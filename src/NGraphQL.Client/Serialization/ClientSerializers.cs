using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NGraphQL.Client.Serialization {

  internal static class ClientSerializers {
    // Deserializer with ExpandoObjectConverter for deserializing data into dynamic object
    internal static JsonSerializer DynamicObjectJsonSerializer;
    // regular strong types serializer
    internal static JsonSerializer TypedJsonSerializer;
    // serializer for variables in URL (GET queries) - non-indented formatting
    internal static JsonSerializerSettings UrlJsonSettings;

    static ClientSerializers() {
      var enumConv = new JsonEnumConverter();

      var dynStt = new JsonSerializerSettings();
      // dynStt.Converters.Add(enumConv); 
      dynStt.Converters.Add(new ExpandoObjectConverter());
      DynamicObjectJsonSerializer = JsonSerializer.Create(dynStt);

      var typedStt = new JsonSerializerSettings();
      typedStt.Formatting = Formatting.Indented;
      typedStt.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
      typedStt.Converters.Add(enumConv);
      TypedJsonSerializer = JsonSerializer.Create(typedStt);

      UrlJsonSettings = new JsonSerializerSettings();
      UrlJsonSettings.Formatting = Formatting.None;
    }


  }
}
