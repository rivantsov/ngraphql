using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Client.Serialization {

  public class EnumInfo {
    public Type Type;
    public bool IsFlagSet;
    public Dictionary<string, EnumValueInfo> ValueInfos = new Dictionary<string, EnumValueInfo>(StringComparer.OrdinalIgnoreCase); //let's be forgiving about casing
    public Func<object, long> ConvertToLong;
    public object NoneValue; 

    public EnumInfo(Type enumType) {
      Type = enumType;
      IsFlagSet = enumType.HasAttribute<FlagsAttribute>();
      ConvertToLong = enumType.GetEnumToLongConverter();
      // build enum value infos
      var values = Enum.GetValues(enumType);
      foreach(var v in values) {
        var vInfo = new EnumValueInfo() {
          Value = v,
          Name = v.ToString().ToUnderscoreUpperCase(),
          LongValue = ConvertToLong(v) 
        };
        if (vInfo.LongValue == 0)
          NoneValue = vInfo.Value;
        ValueInfos.Add(vInfo.Name, vInfo);
      }
      if (NoneValue == null)
        NoneValue = Activator.CreateInstance(enumType);
    }
  }

  public class EnumValueInfo {
    public object Value;
    public string Name; //GraphQL name
    public long LongValue;
  }

}
