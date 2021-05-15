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

  public static class ServerReflectionHelper {

    private static string[] _specialMethods = new string[] { "ToString", "Equals", "GetHashCode", "GetType" };

    public static IList<MemberInfo> GetFieldsPropsMethods(this Type type, bool withMethods) {
      var mTypes = MemberTypes.Field | MemberTypes.Property;
      if (withMethods)
        mTypes |= MemberTypes.Method;
      var members = type.GetAllPublicMembers()
        .Where(m => !_specialMethods.Contains(m.Name))
        .Where(m => (m.MemberType & mTypes) != 0)
        .Where(m => !(m is MethodInfo mi && mi.IsSpecialName)) //filter out getters/setters
        .ToList();
      return members;
    }

    public static List<MethodInfo> GetPublicMethods(this Type type) {
      var methods = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.MemberType == MemberTypes.Method)
        .Where(m => m.DeclaringType != typeof(object))
        .Where(m => !(m is MethodInfo mi && mi.IsSpecialName)) //filter out getters/setters
        .OfType<MethodInfo>()
        .ToList();
      return methods;
    }


    public static bool MethodReturnsTask(this MethodInfo method) {
      var retType = method.ReturnType;
      var returnsTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
      return returnsTask; 
    }

    public static Type GetReturnDataType(this MethodInfo method) {
      var retType = method.ReturnType;
      if(retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>))
        retType = retType.GetGenericArguments()[0];
      return retType;
    }


    // Note: interfaces are special; if the entity type is interface (as in VITA, db entities are defined as interfaces),
    //  getting all members must account for this; GetMembers does NOT return members of base interfaces, so we do it explicitly
    private static IList<MemberInfo> GetAllPublicMembers(this Type type) {
      var flags = BindingFlags.Public | BindingFlags.Instance;
      var members = type.GetMembers(flags).ToList();
      if (type.IsInterface) {
        var interfaces = type.GetInterfaces();
        foreach (var intf in interfaces) 
          members.AddRange(intf.GetMembers(flags));
      }
      return members; 
    }

    public static IList<MemberInfo> GetFieldsProps(this Type type) {
      return type.GetFieldsPropsMethods(withMethods: false); 
    }

    public static Type GetMemberReturnType(this MemberInfo member) {
      switch (member) {
        case PropertyInfo pi:
          return pi.PropertyType;
        case FieldInfo fi:
          return fi.FieldType;
        case MethodInfo mi:
          var retType = mi.ReturnType;
          if (retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>))
            retType = retType.GetGenericArguments()[0];
          return retType; 
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
