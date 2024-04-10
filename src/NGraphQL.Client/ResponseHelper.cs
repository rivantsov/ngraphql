using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Model;
using NGraphQL.Utilities;

namespace NGraphQL.Client {

  public static class ResponseHelper {

    public static void EnsureNoErrors(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return;
      var errText = response.GetErrorsAsText();
      var msg = "Request failed.";
      if (!string.IsNullOrWhiteSpace(errText))
        msg += " Error(s):" + Environment.NewLine + errText;
      throw new Exception(msg);
    }

    public static string GetErrorsAsText(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return string.Empty;
      var text = string.Join(Environment.NewLine, response.Errors);
      return text;
    }

    internal static JObject GetDataJObject(this ServerResponse response) {
      // read 'data' object as JObject 
      if (response.TopFields.TryGetValue("data", out var data))
        return data as JObject;
      return null;
    }

    public static T GetTopField<T>(this ServerResponse response, string name) {
      var dataJObj = response.GetDataJObject();
      if (dataJObj == null)
        throw new Exception("'data' element was not returned by the request. See errors in response.");
      if (!dataJObj.TryGetValue(name, out var jtoken))
        throw new Exception($"Field '{name}' not found in response.");
      var type = typeof(T);
      var nullable = ReflectionHelper.CheckNullable(ref type);
      if (jtoken == null) {
        if (nullable)
          return (T)(object)null;
        throw new Exception($"Field '{name}': cannot convert null value to type {typeof(T)}.");
      }
      if (jtoken is JValue jv && !type.IsValueType)
        return (T)jv.Value;
      // deserialize as type
      var res = jtoken.ToObject<T>(ClientSerializers.TypedJsonSerializer);
      return res;
    }

    public static TEnum ToEnum<TEnum>(object value) {
      var enumType = typeof(TEnum);
      if (!enumType.IsEnum)
        throw new Exception($"Invalid type argument '{enumType}', expected enum.");
      var handler = EnumHandlersCache.GetEnumHandler(enumType);
      if (handler.IsFlagSet) {
        if (!(value is IList<string> stringList))
          stringList = ((IList)value).OfType<string>().ToList(); 
        return (TEnum) handler.ConvertStringListToFlagsEnumValue(stringList);
      } else
        return (TEnum) handler.ConvertStringToEnumValue((string)value);
    }

  }
}
