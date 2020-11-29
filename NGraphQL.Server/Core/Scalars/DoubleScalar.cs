using System;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class DoubleScalar : Scalar {

    public DoubleScalar() : base("Double", "Double scalar", typeof(double), isCustom: true) {
      CanConvertFrom = new[] { typeof(Single), typeof(int), typeof(long), typeof(Decimal) }; 
    }

    public override object ParseToken(IScalarContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          if(double.TryParse(token.Text, out var value))
            return value;
          break;
      }
      context.ThrowScalarError($"Invalid double value: '{token.Text}'", token);
      return null; 
    }

    public override object ConvertInputValue(object value) {
      switch(value) {
        case null: return null;
        case Single s: return (double) s;
        case double d: return d;
        case int i: return (double)i;
        case long lng: return (double)lng;
        case decimal dec: return (double)dec; 
        default:
          throw new Exception($"Invalid Double value: '{value}'");
      }
    }
  }
}
