using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NGraphQL.Client.Json {

  // based on code from here: 
  //    https://stackoverflow.com/questions/65972825/c-sharp-deserializing-nested-json-to-nested-dictionarystring-object
  // this converter is used mostly for reading insided Maps/Dictionaries, when value under a key is an object

  public class ObjectAsPrimitiveConverter : JsonConverter<object> {

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
      if (value.GetType() == typeof(object)) {
        writer.WriteStartObject();
        writer.WriteEndObject();
      } else {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
      }
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      switch (reader.TokenType) {
        case JsonTokenType.Null:
          return null;
        case JsonTokenType.False:
          return false;
        case JsonTokenType.True:
          return true;
        case JsonTokenType.String:
          return reader.GetString();
        case JsonTokenType.Number: {
            if (reader.TryGetInt32(out var i))
              return i;
            if (reader.TryGetInt64(out var l))
              return l;
            // BigInteger could be added here.
            if (reader.TryGetDouble(out var d))
              return d;
            var str = reader.GetString(); 
            throw new JsonException($"Cannot parse number: '{str}'");
          }
        case JsonTokenType.StartArray: {
            var list = new List<object>();
            while (reader.Read()) {
              switch (reader.TokenType) {
                default:
                  list.Add(Read(ref reader, typeof(object), options));
                  break;
                case JsonTokenType.EndArray:
                  return list;
              }
            }
            throw new JsonException();
          }
        case JsonTokenType.StartObject:
          var dict = new Dictionary<string, object>();
          while (reader.Read()) {
            switch (reader.TokenType) {
              case JsonTokenType.EndObject:
                return dict;
              case JsonTokenType.PropertyName:
                var key = reader.GetString();
                reader.Read();
                dict.Add(key, Read(ref reader, typeof(object), options));
                break;
              default:
                throw new JsonException();
            }
          }
          throw new JsonException();
        default:
          throw new JsonException(string.Format("Unknown token {0}", reader.TokenType));
      }
    }

  }
}
