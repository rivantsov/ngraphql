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

    public T Convert<T>(dynamic value) where T: Enum {
      var enumInfo = GetEnumInfo(typeof(T));
      if (enumInfo == null)
        throw new Exception($"Type {typeof(T)} is not enum, expected enum type."); 
      switch(value) {
        case null:
          break; 
      }
      if (enumInfo.IsFlagSet) {

      } else {

      }
      return default;
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
