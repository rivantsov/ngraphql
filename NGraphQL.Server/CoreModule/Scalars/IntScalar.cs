using System;

using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class IntScalar : Scalar {

    public IntScalar() : base("Int", "Int scalar", typeof(int), isCustom: false) {
      CanConvertFrom = new[] { typeof(long) };
    }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          return token.ParsedValue;  //relying on converting by parser, including hex conversion
      }
      context.ThrowScalarError($"Invalid int value: '{token.Text}'", token);
      return null;
    }

    // value is garanteed to be not null
    public override object ConvertInputValue(object value) {
      switch(value) {
        case null: return null;
        case int i: return i; 
        case long lng: return (int)lng;

        case byte _:
        case sbyte _:
        case Int16 _:
        case UInt16 _:
        case UInt32 _:
        case ulong _:
          return Convert.ChangeType(value, typeof(Int32));

        default:
          throw new Exception($"Invalid Int value: '{value}'"); 
      }
    }
  }
}
