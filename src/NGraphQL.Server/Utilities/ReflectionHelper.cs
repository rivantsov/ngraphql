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

    private static string[] _specialMethods = new string[] { "ToString", "Equals", "GetHashCode", "GetType" };

    public static IList<MemberInfo> GetFieldsPropsMethods(this Type type, bool withMethods) {
      var mTypes = MemberTypes.Field | MemberTypes.Property;
      if (withMethods)
        mTypes |= MemberTypes.Method;
      var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => !_specialMethods.Contains(m.Name))
        .Where(m => (m.MemberType & mTypes) != 0)
        .Where(m => !(m is MethodInfo mi && mi.IsSpecialName)) //filter out getters/setters
        .ToList();
      return members;
    }

    public static IList<MemberInfo> GetFieldsProps(this Type type) {
      return type.GetFieldsPropsMethods(withMethods: false); 
    }

    public static List<MethodInfo> GetResolverMethods(this Type type, string name) {
      var methods = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.MemberType == MemberTypes.Method)
        .Where(m => m.DeclaringType != typeof(object))
        .OfType<MethodInfo>()
        .ToList();
      return methods;
    }

    public static Type GetMemberReturnType(this MemberInfo member) {
      switch (member) {
        case PropertyInfo pi:
          return pi.PropertyType;
        case FieldInfo fi:
          return fi.FieldType;
        case MethodInfo mi:
          return mi.ReturnType;
      }
      throw new Exception($"Invalid argument for {nameof(GetMemberReturnType)}: {member.Name}");
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
