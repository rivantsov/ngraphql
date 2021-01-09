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
    // Note: dynamic object do not allow externsion methods, so don't try putting 'this' before parameter
    public static T ToEnum<T>(object dynamicValue)  {
      return EnumConverter.Instance.Convert<T>(dynamicValue); 
    }

  }
}
