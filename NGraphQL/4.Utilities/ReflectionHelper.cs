using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NGraphQL.Model;

namespace NGraphQL.Utilities {

  public static class ReflectionHelper {

    public static object GetDefaultValue(this Type type) {
      if (type.IsValueType)
        return Activator.CreateInstance(type);
      else
        return null; 
    }

    public static string GetTypeName(this Type type) {
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
        var t = type.GetGenericArguments()[0];
        return t.Name + "?";
      }
      return type.Name; 
    }

    public static Type GetMemberType(this MemberInfo member) {
      switch (member) {
        case PropertyInfo pi:
          return pi.PropertyType;
        case FieldInfo fi:
          return fi.FieldType;
      }
      throw new Exception($"Invalid argument for GetMemberReturnType: {member.Name}");
    }

    public static void SetMember(this MemberInfo member, object obj, object value) {
      switch (member) {
        case PropertyInfo pi:
          pi.SetValue(obj, value);
          break;
        case FieldInfo fi:
          fi.SetValue(obj, value);
          break;
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


    // detects list and its rank
    public static bool IsGenericListOrArray(this Type type, out Type elemType, out int rank) {
      rank = 0;
      elemType = null;
      var maybeListType = type;
      while (IsGenericListOrArray(maybeListType, out var listElemType)) {
        rank++;
        elemType = listElemType; // for return
        maybeListType = listElemType; // for next iteration
      }
      return rank > 0;
    }

    public static bool IsGenericListOrArray(this Type type, out Type elemType) {
      elemType = null;
      if (type.IsValueType)
        return false;
      if (type.IsArray) {
        elemType = type.GetElementType();
        return true;
      }
      if (!type.IsGenericType)
        return false;
      var genType = type.GetGenericTypeDefinition();
      if (genType.GetGenericArguments().Length != 1)
        return false;
      var result = genType == typeof(List<>) || genType == typeof(IList<>) || genType == typeof(ICollection<>);
      if (result)
        elemType = type.GetGenericArguments()[0];
      return result;
    }

    public static IList<MemberInfo> GetFieldsProps(this Type type) {
      return type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                   .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                   .ToList();
    }

    public static Func<object, object> CompileTaskResultReader(Type resultType) {
      var taskType = typeof(Task<>).MakeGenericType(resultType);
      var resultProp = taskType.GetProperty("Result");
      var prm = Expression.Parameter(typeof(object));
      var taskExpr = Expression.Convert(prm, taskType);
      var readPropExpr = Expression.MakeMemberAccess(taskExpr, resultProp);
      var resultExpr = Expression.Convert(readPropExpr, typeof(object));
      var lambda = Expression.Lambda(resultExpr, prm);
      var func = (Func<object, object>)lambda.Compile();
      return func;
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

    public static bool IsEnumType(this Type type ) {
      if (type.IsEnum)
        return true;
      var nt = Nullable.GetUnderlyingType(type);
      if (nt != null && nt.IsEnum)
        return true;
      return false; 
    }

    public static IList CreateTypedArray(this Type elemType, int length) {
      return Array.CreateInstance(elemType, length);
    }


  }
}
