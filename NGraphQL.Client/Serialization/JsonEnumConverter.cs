using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NGraphQL.Client.Serialization {

  internal class JsonEnumConverter : JsonConverter {
    EnumConverter _conv = EnumConverter.Instance;

    // We want to handle enum, enum? types. For arrays of enums, we do not want to interfere, the main serializer will handle 
    // array, and only come here for individual values.  
    public override bool CanConvert(Type objectType) {
      if (objectType.IsEnum)
        return true;
      if (!objectType.IsValueType)
        return false; 
      // try lookup and register
      var enumInfo = _conv.GetEnumInfo(objectType); 
      return enumInfo != null; 
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      var enumType = objectType; 
      var nullable = ClientExtensions.CheckNullable(ref enumType);
      if (!enumType.IsEnum)
        throw new Exception($"{nameof(JsonEnumConverter)}: unexpected conversion type {enumType}; expected enum.");
      var enumInfo = _conv.GetEnumInfo(enumType);
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
            var objArr = jArr.Select(v => (object) v.ToString()).ToArray();
            var res = _conv.Convert(objArr, objectType);
            reader.Skip();
            return res; 
          default:
            throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for Flags enum type {objectType}, expected string array.");
        }
      } else {
        switch(tokenReader.CurrentToken) {
          case JValue jv:
            if (!(jv.Value is string s))
              throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for enum type {objectType}, expected string.");
            var res = _conv.Convert(s, objectType);
            return res;
          default:
            throw new Exception($"{nameof(JsonEnumConverter)}: invalid input value for enum type {objectType}, expected string.");
        } //switch
      } //else
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      throw new NotImplementedException(); //should never be called
    }
  }
}
