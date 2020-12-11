using System;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class LongScalar : Scalar {

    public LongScalar() : base("Long", "Long scalar", typeof(long), isCustom: true) {
      CanConvertFrom = new[] { typeof(int) };
    }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          return token.ParsedValue;  //relying on converting by parser, including hex conversion
      }
      context.ThrowScalarError($"Invalid long value: '{token.Text}'", token);
      return null;
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case null: return null;
        case int i: return (long) i;
        case long lng: return lng;
        case ulong ulng: return (long)ulng;

        case byte _:
        case sbyte _:
        case Int16 _:
        case UInt16 _:
        case UInt32 _:
          return Convert.ChangeType(value, typeof(long));

        default:
          throw new Exception($"Invalid Long value: '{value}'");
      }
    }

  }
}
