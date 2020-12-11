using System;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class DecimalScalar : Scalar {

    public DecimalScalar() : base("Decimal", "Decimal scalar", typeof(decimal)) {
      CanConvertFrom = new[] { typeof(Single), typeof(double), typeof(int), typeof(long) };
    }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          if(decimal.TryParse(token.Text, out var value))
            return value;
          break;
      }
      context.ThrowScalarError($"Invalid int value: '{token.Text}'", token);
      return null;
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case null: return null;
        case decimal dec: return dec;
        case Single s: return (decimal)s;
        case double d: return (decimal) d;
        case int i: return (decimal)i;
        case long lng: return (decimal)lng;
        default:
          throw new Exception($"Invalid Decimal value: '{value}'");
      }
    }

  }
}
