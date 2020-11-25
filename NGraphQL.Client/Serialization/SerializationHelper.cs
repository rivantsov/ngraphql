using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NGraphQL.Client.Serialization {
  internal static class SerializationHelper {

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
