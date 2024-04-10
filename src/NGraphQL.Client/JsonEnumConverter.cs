using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using NGraphQL.Model;
using NGraphQL.Utilities;

namespace NGraphQL.Client {

  internal class JsonEnumConverter : JsonConverterFactory {

    // We want to handle enum, enum? types. For arrays of enums, we do not want to interfere, the main serializer will handle 
    // array, and only come here for individual values.  
    public override bool CanConvert(Type objectType) {
      if (objectType.IsEnum)
        return true;
      if (!objectType.IsValueType)
        return false; 
      // try lookup and register
      var handler = EnumHandlersCache.GetEnumHandler(objectType); 
      return handler != null; 
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      var enumType = objectType; 
      var nullable = ReflectionHelper.CheckNullable(ref enumType);
      if (!enumType.IsEnum)
        throw new Exception($"{nameof(JsonEnumConverter)}: unexpected conversion type {enumType}; expected enum.");
      var enumHandler = EnumHandlersCache.GetEnumHandler(enumType);
      var enumInfo = EnumHandlersCache.GetEnumHandler(enumType);
      // check null
      if (reader.TokenType == JsonToken.Null) {
        if (!nullable)
          throw new Exception($"{nameof(JsonEnumConverter)}: input value null cannot be converted to type {enumType}.");
      }
      var tokenReader = (JTokenReader)reader; 
      if (enumInfo.IsFlagSet) {
        switch(tokenReader.CurrentToken) {
          case JArray jArr:
            if (jArr.Count == 0)
              return enumInfo.NoneValue;
            var strings = jArr.Select(v => v.ToString()).ToArray();
            var res = enumHandler.ConvertStringListToFlagsEnumValue(strings);
            reader.Skip();
            return res; 
          default:
            throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for Flags enum type {enumType}, expected string array.");
        }
      } else {
        switch(tokenReader.CurrentToken) {
          case JValue jv:
            if (!(jv.Value is string s))
              throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for enum type {objectType}, expected string.");
            var res = enumHandler.ConvertStringToEnumValue(s);
            return res;
          default:
            throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for enum type {objectType}, expected string.");
        } //switch
      } //else
    }

    // Used in serializing enums on the client
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (value == null) {
        writer.WriteNull();
        return; 
      }
      var enumType = value.GetType();
      if (!enumType.IsEnum)
        throw new Exception($"{nameof(JsonEnumConverter)}: unexpected conversion type {enumType}; expected enum.");
      var handler = EnumHandlersCache.GetEnumHandler(enumType);
      if (handler.IsFlagSet) {
        writer.WriteStartArray();
        var strings = handler.ConvertFlagsEnumValueToOutputStringList(value);
        foreach (var sv in strings)
          writer.WriteValue(sv);
        writer.WriteEndArray(); 
      } else {
        var sv = handler.ConvertEnumValueToOutputString(value);
        writer.WriteValue(sv); 
      }

    }//method
  }
}
