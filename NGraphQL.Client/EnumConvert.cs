using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Client.Serialization;

namespace NGraphQL.Client {

  public static class EnumConvert {

    /// <summary>Converts a dynamic value from GraphQL response serialized as dynamic data into enum value. </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    /// <param name="dynamicValue">The value to convert.</param>
    /// <returns></returns>
    public static T ToEnum<T>(this object dynamicValue)  {
      return EnumValuesConverter.Instance.Convert<T>(dynamicValue); 
    }

  }
}
