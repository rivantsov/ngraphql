using System;
using System.Collections.Generic;
using NGraphQL.Model;

namespace NGraphQL.Client.Serialization {
  public static class KnownEnumTypes {

    static Dictionary<Type, EnumHandler> _enumsInfoLookup = new Dictionary<Type, EnumHandler>();

    public static EnumHandler GetEnumHandler(Type enumType) {
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

    // private methods 
    static object _lock = new object();

    private static EnumHandler RegisterEnum(Type enumType) {
      var enumInfo = new EnumHandler(enumType);
      lock (_lock) {
        // copy-add-replace
        var newDict = new Dictionary<Type, EnumHandler>(_enumsInfoLookup);
        newDict[enumType] = enumInfo;
        _enumsInfoLookup = newDict;
      }
      return enumInfo;
    }


  }
}
