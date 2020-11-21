using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NGraphQL.Client {
  public class EnumConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
      if (objectType.IsEnum)
        return true;
      if (!objectType.IsValueType)
        return false;
      // check nullable
      var underType = Nullable.GetUnderlyingType(objectType);
      return underType != null && underType.IsEnum; 
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      throw new NotImplementedException(); //should never be called
    }
  }
}
