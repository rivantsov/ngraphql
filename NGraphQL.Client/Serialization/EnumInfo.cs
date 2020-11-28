using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Client.Utilities;

namespace NGraphQL.Client.Serialization {

  internal class EnumInfo {
    public Type Type;
    public bool IsFlagSet;
    public Dictionary<string, EnumValueInfo> ValueInfos = 
         new Dictionary<string, EnumValueInfo>(StringComparer.OrdinalIgnoreCase); //let's be forgiving about casing
    public Func<object, long> ConvertToLong;
    public object NoneValue; 

    public EnumInfo(Type enumType) {
      Type = enumType;
      IsFlagSet = enumType.HasAttribute<FlagsAttribute>();
      ConvertToLong = GetEnumToLongConverter(enumType);
      // build enum value infos
      var values = Enum.GetValues(enumType);
      foreach(var v in values) {
        var vInfo = new EnumValueInfo() {
          Value = v,
          Name = ToUnderscoreUpperCase(v.ToString()),
          LongValue = ConvertToLong(v) 
        };
        if (vInfo.LongValue == 0)
          NoneValue = vInfo.Value;
        ValueInfos.Add(vInfo.Name, vInfo);
      }
      if (NoneValue == null)
        NoneValue = Activator.CreateInstance(enumType);
    }

    private static string ToUnderscoreUpperCase(string value) {
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

    private static Func<object, long> GetEnumToLongConverter(Type enumType) {
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

  }

  internal class EnumValueInfo {
    public object Value;
    public string Name; //GraphQL name
    public long LongValue;
  }

}
