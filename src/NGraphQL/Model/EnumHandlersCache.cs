using System;
using System.Collections.Generic;

namespace NGraphQL.Model {

  public static class EnumHandlersCache {

    static Dictionary<Type, EnumHandler> _handlers = new Dictionary<Type, EnumHandler>();

    public static EnumHandler GetEnumHandler(Type enumType) {
      if (enumType.IsEnum && _handlers.TryGetValue(enumType, out var enumInfo))
        return enumInfo;
      if (!enumType.IsValueType)
        return null;
      var underType = Nullable.GetUnderlyingType(enumType);
      if (underType != null) {
        enumType = underType;
        // lookup again
        if (enumType.IsEnum && _handlers.TryGetValue(enumType, out enumInfo))
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
        var newDict = new Dictionary<Type, EnumHandler>(_handlers);
        newDict[enumType] = enumInfo;
        _handlers = newDict;
      }
      return enumInfo;
    }


  }
}
