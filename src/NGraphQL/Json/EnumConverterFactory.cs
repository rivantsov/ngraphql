using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NGraphQL.Model;
using NGraphQL.Utilities;

namespace NGraphQL.Json {

  internal class EnumConverterFactory : JsonConverterFactory {

    // We want to handle enum, enum? types. For arrays of enums, we do not want to interfere, the main serializer will handle 
    // array, and only come here for individual values.  
    public override bool CanConvert(Type objectType) {
      if (!objectType.IsValueType)
        return false;
      ReflectionHelper.CheckNullable(ref objectType);
      return objectType.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
      var convType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
      var conv = (JsonConverter)Activator.CreateInstance(convType, options);
      return conv;    
    }
  } // class

  // Handles both Enum and Enum? types for a concrete enum
  public class EnumConverter<TEnum> : JsonConverter<TEnum> {
    EnumHandler _enumHandler;
    JsonSerializerOptions _options; 

    public EnumConverter(JsonSerializerOptions options) {
      _options = options; 
      _enumHandler = EnumHandlersCache.GetEnumHandler(typeof(TEnum));
    }
    
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var enumType = typeToConvert; 
      var nullable = ReflectionHelper.CheckNullable(ref enumType);
      if (!enumType.IsEnum)
        throw new Exception($"{nameof(EnumConverterFactory)}: unexpected conversion type {enumType}; expected enum.");
      // check null
      if (reader.TokenType == JsonTokenType.Null) {
        if (!nullable)
          throw new Exception($"{nameof(EnumConverterFactory)}: input value null cannot be converted to type {enumType}.");
        return default; // it is null, since type is enum?
      }
      if (_enumHandler.IsFlagSet) {
        switch (reader.TokenType) {
            
          case JsonTokenType.StartArray:
            reader.Read();
            var strings = new List<string>();
            while (reader.TokenType == JsonTokenType.String) {
              var s = reader.GetString();
              strings.Add(s);
              reader.Read(); 
            }
            var res = _enumHandler.ConvertStringListToFlagsEnumValue(strings);
            return (TEnum) res; 
          default:
            throw new Exception($"{nameof(EnumConverterFactory)}: invalid input value for Flags enum type {enumType}, expected string array.");
        }
      } else {
        switch (reader.TokenType) {
          case JsonTokenType.String:
            var str = reader.GetString();
            var res = _enumHandler.ConvertStringToEnumValue(str);
            return (TEnum) res;
          default:
            throw new Exception($"{nameof(EnumConverterFactory)}: invalid input value for enum type {enumType}, expected string.");
        } //switch
      } //else
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) {
      //if (value == null) {
      //  writer.WriteNull();
      //  return;
      //}
      var enumType = value.GetType();
      if (!enumType.IsEnum)
        throw new Exception($"{nameof(EnumConverterFactory)}: unexpected conversion type {enumType}; expected enum.");
      if (_enumHandler.IsFlagSet) {
        writer.WriteStartArray();
        var strings = _enumHandler.ConvertFlagsEnumValueToOutputStringList(value);
        foreach (var sv in strings)
          writer.WriteStringValue(sv);
        writer.WriteEndArray();
      } else {
        var sv = _enumHandler.ConvertEnumValueToOutputString(value);
        writer.WriteStringValue(sv);
      }

    }



  }
  /*
  // Used in serializing enums on the client
  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
    if (value == null) {
      writer.WriteNull();
      return; 
    }
    var enumType = value.GetType();
    if (!enumType.IsEnum)
      throw new Exception($"{nameof(EnumConverterFactory)}: unexpected conversion type {enumType}; expected enum.");
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

  */
}
