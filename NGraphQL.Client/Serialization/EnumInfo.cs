using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Client.Serialization {

  public class EnumInfo {
    public Type Type;
    public bool IsFlagSet;
    public Dictionary<string, EnumValueInfo> ValueInfos = new Dictionary<string, EnumValueInfo>();
    public Func<object, long> ConvertToLong;

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
        ValueInfos.Add(vInfo.Name, vInfo);
      }
    }
  }

  public class EnumValueInfo {
    public object Value;
    public string Name; //GraphQL name
    public long LongValue;
  }

}
