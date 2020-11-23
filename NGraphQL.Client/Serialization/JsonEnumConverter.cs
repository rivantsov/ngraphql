using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NGraphQL.Client.Serialization {

  public class JsonEnumConverter : JsonConverter {
    EnumValuesConverter _converter = new EnumValuesConverter();

    // We want to handle enum, enum? types. For arrays of enums, we do not want to interfere, the main serializer will handle 
    // array, and only come here for individual values.  
    public override bool CanConvert(Type objectType) {
      if (objectType.IsEnum)
        return true;
      if (!objectType.IsValueType)
        return false; 
      // try lookup and register
      var enumInfo = _converter.GetEnumInfo(objectType); 
      return enumInfo != null; 
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      var enumInfo = _converter.GetEnumInfo(objectType);
      if (enumInfo == null)
        throw new Exception($"{nameof(JsonEnumConverter)} cannot handler object of type {objectType}; failed to lookup EnumInfo.");
      if(enumInfo.IsFlagSet) {

      } else {

      }
      return null; 
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      throw new NotImplementedException(); //should never be called
    }
  }
}
