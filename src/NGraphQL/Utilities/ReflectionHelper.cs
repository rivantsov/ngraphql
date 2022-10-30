using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NGraphQL.Utilities {

  public static class ReflectionHelper {

    public static object GetDefaultValue(this Type type) {
      if (type.IsValueType)
        return Activator.CreateInstance(type);
      else
        return null;
    }
    public static string GetTypeName(this Type type) {
      if (type.IsGenericType && Nullable.GetUnderlyingType(type) != null) {
        var t = type.GetGenericArguments()[0];
        return t.Name + "?";
      }
      return type.Name;
    }

    public static bool HasAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      return provider.GetAttribute<TAttr>() != null;
    }

    public static TAttr GetAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attr = provider.GetAttributes<TAttr>().FirstOrDefault();
      return attr;
    }

    public static IList<TAttr> GetAttributes<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attrs = provider.GetCustomAttributes(inherit: true).Where(a => a is TAttr).OfType<TAttr>().ToList();
      return attrs;
    }

    public static TAttr Find<TAttr>(this IList<Attribute> attrs) where TAttr : Attribute {
      return attrs.FirstOrDefault(a => a.GetType() == typeof(TAttr)) as TAttr;
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

    internal static Func<object, long> GetEnumToLongConverter(this Type enumType) {
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

    public static object GetMemberValue(this MemberInfo member, object obj) {
      switch (member) {
        case PropertyInfo pi:
          return pi.GetValue(obj);
        case FieldInfo fi:
          return fi.GetValue(obj);
      }
      return null;
    }

  }
}
