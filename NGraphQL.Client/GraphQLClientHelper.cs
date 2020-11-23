using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace NGraphQL.Client {
  using IDict = IDictionary<string, object>;

  public static class GraphQLClientHelper {

    public static void EnsureNoErrors(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return;
      var errText = response.GetErrorsAsText();
      var msg = "Request failed.";
      if (!string.IsNullOrWhiteSpace(errText))
        msg += " Error(s):" + Environment.NewLine + errText;
      throw new Exception(msg);
    }

    public static bool CheckNullable(ref Type type) {
      if (!type.IsValueType)
        return true;   
      var underType = Nullable.GetUnderlyingType(type);
      if (underType != null) {
          type = underType;
          return true;
      }
      return false;       
    }

    public static string GetErrorsAsText(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return string.Empty;
      var text = string.Join(Environment.NewLine, response.Errors);
      return text; 
    }

    public static Func<object, long> GetEnumToLongConverter(this Type enumType) {
      if (!enumType.IsEnum)
        throw new Exception($"Invalid type {enumType}, expected enum.");
      var baseType = Enum.GetUnderlyingType(enumType);
      switch (baseType.Name) {
        case nameof(Int32):
          return (v) => (long)(int)v;
        case nameof(Int64):
          return (v) => (long)v;
        default:
          throw new Exception($"Enum {enumType}: unsupported base type {baseType}.");
      }
    }

    public static bool HasAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      return provider.GetAttribute<TAttr>() != null;
    }

    public static IList<TAttr> GetAttributes<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attrs = provider.GetCustomAttributes(inherit: true).Where(a => a is TAttr).OfType<TAttr>().ToList();
      return attrs;
    }
    public static TAttr GetAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attr = provider.GetAttributes<TAttr>().FirstOrDefault();
      return attr;
    }

    public static string ToUnderscoreUpperCase(this string value) {
      if (string.IsNullOrEmpty(value))
        return value;
      var chars = value.ToCharArray();
      char prevCh = '\0';
      var newChars = new List<char>();
      foreach (var ch in chars) {
        if (char.IsUpper(ch)) {
          if (newChars.Count > 0 && prevCh != '_' && !char.IsUpper(prevCh)) //avoid double-underscores
            newChars.Add('_');
          newChars.Add(ch);
        } else
          newChars.Add(ch);
        prevCh = ch;
      }
      var result = new string(newChars.ToArray()).Replace("__", "_"); //cleanup double _, just in case
      result = result.ToUpperInvariant();
      return result;
    }

  }
}
