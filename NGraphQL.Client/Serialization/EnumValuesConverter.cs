using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Client.Serialization {
  internal class EnumValuesConverter {
    public static readonly EnumValuesConverter Instance = new EnumValuesConverter();  

    Dictionary<Type, EnumInfo> _enumsInfoLookup = new Dictionary<Type, EnumInfo>();

    public EnumInfo GetEnumInfo(Type enumType) {
      if (enumType.IsEnum && _enumsInfoLookup.TryGetValue(enumType, out var enumInfo))
        return enumInfo;
      if (!enumType.IsValueType)
        return null;
      var underType = Nullable.GetUnderlyingType(enumType);
      if (underType != null) {
        enumType = underType;
        // lookup again
        if (enumType.IsEnum && _enumsInfoLookup.TryGetValue(enumType, out enumInfo))
          return enumInfo;
      }
      if (!enumType.IsEnum)
        return null;
      enumInfo = RegisterEnum(enumType);
      return enumInfo;
    }

    public T Convert<T>(object value) {
      var type = typeof(T);
      var nullable = GraphQLClientHelper.CheckNullable(ref type);
      var enumInfo = GetEnumInfo(type);
      if (enumInfo == null)
        throw new Exception($"Type {typeof(T)} is not enum, expected enum type."); 
      switch(value) {
        case null:
          if (nullable)
            return default;
          throw new Exception($"Enum conversion error: failed to convert null to type {typeof(T)}.");
        case string sValue:
          if (enumInfo.ValueInfos.TryGetValue(sValue, out var enumValue))
            return (T) enumValue.Value;
          throw new Exception($"Enum conversion error: failed to convert string '{sValue}' to enum {type}.");
        case IList<object> vArr:
          if (!enumInfo.IsFlagSet) {
            var strArr = string.Join(", ", vArr);
            throw new Exception($"Enum conversion error: failed to convert array '[{strArr}]' to enum {type}.");
          }
          return ConvertFlagsEnum<T>(enumInfo, vArr);
        default:
          var vType = value.GetType(); 
          throw new Exception($"Enum conversion error: failed to convert value '{value}' ({vType}) to enum {type}.");
      } //switch value
    }

    private T ConvertFlagsEnum<T>(EnumInfo enumInfo, IList<object> values) {
      long result = 0;
      if (values.Count == 0)
        return (T)enumInfo.NoneValue;
      foreach (var v in values) {
        if (!(v is string vStr))
          throw new Exception($"Enum conversion error: failed to convert array to Flags enum {enumInfo.Type}, invalid element {v} in input array.");
        if(!enumInfo.ValueInfos.TryGetValue(vStr, out var vInfo))
          throw new Exception($"Enum conversion error: failed to convert array to Flags enum {enumInfo.Type}, {vStr} in input array does not match any enum members.");
        result |= vInfo.LongValue;
      }
      // convert long to enum;
      var baseType = enumInfo.Type.GetEnumUnderlyingType();
      if (baseType == typeof(int))
        return (T)(object)(int)result;
      if (baseType == typeof(Int64))
        return (T)(object)result;
      return (T) Enum.Parse(typeof(T), result.ToString()); //Parse can do this
    }

    // private methods 
    object _lock = new object();

    private EnumInfo RegisterEnum(Type enumType) {
      var enumInfo = new EnumInfo (enumType);
      lock (_lock) {
        // copy-add-replace
        var newDict = new Dictionary<Type, EnumInfo>(_enumsInfoLookup);
        newDict[enumType] = enumInfo;
        _enumsInfoLookup = newDict; 
      }
      return enumInfo; 
    }

  }
}
