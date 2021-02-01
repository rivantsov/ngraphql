using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Internals;

namespace NGraphQL.Internals {

  public static class EnumConvert {

    /// <summary>Converts a dynamic value from GraphQL response serialized as dynamic data into enum value. </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    /// <param name="dynamicValue">The value to convert.</param>
    /// <returns></returns>
    // Note: dynamic object do not allow externsion methods, so don't try putting 'this' before parameter
    public static T ToEnum<T>(object dynamicValue) {
      return EnumConverter.Instance.Convert<T>(dynamicValue);
    }


    public static T Convert<T>(object value) {
      return (T)Convert(value, typeof(T));
    }

    public static object StringToEnum(EnumHandler enumInfo, object value) {
      var nullable = ReflectionHelper.CheckNullable(ref type);
      if (enumInfo == null)
        throw new Exception($"Type {type} is not enum, expected enum type."); 
      switch(value) {
        case null:
          if (nullable)
            return default;
          throw new Exception($"Enum conversion error: failed to convert null to type {type}.");
        case string sValue:
          if (enumInfo.ValuesLookup.TryGetValue(sValue, out var enumValue))
            return enumValue.Value;
          throw new Exception($"Enum conversion error: failed to convert string '{sValue}' to enum {type}.");
        case IList<object> vArr:
          if (!enumInfo.IsFlagSet) {
            var strArr = string.Join(", ", vArr);
            throw new Exception($"Enum conversion error: failed to convert array '[{strArr}]' to enum {type}.");
          }
          return ConvertFlagsEnum(enumInfo, vArr);
        default:
          var vType = value.GetType(); 
          throw new Exception($"Enum conversion error: failed to convert value '{value}' ({vType}) to enum {type}.");
      } //switch value
    }

    public object ConvertFlagsEnum(EnumHandler enumInfo, IList<object> values) {
      long result = 0;
      if (values.Count == 0)
        return enumInfo.NoneValue;
      foreach (var v in values) {
        if (!(v is string vStr))
          throw new Exception($"Enum conversion error: failed to convert array to Flags enum {enumInfo.Type}, invalid element {v} in input array.");
        if(!enumInfo.ValuesLookup.TryGetValue(vStr, out var vInfo))
          throw new Exception($"Enum conversion error: failed to convert array to Flags enum {enumInfo.Type}, {vStr} in input array does not match any enum members.");
        result |= vInfo.LongValue;
      }
      // convert long to enum;
      return Enum.ToObject(enumInfo.Type, result); 
    }

  }
}
