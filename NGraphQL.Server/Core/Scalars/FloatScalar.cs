using System;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class FloatScalar : Scalar {

    public FloatScalar() : base("Float", "Float scalar", typeof(Single), isCustom: false) {
      CanConvertFrom = new[] { typeof(Double), typeof(int), typeof(long), typeof(Decimal) };
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
      context.ThrowScalarError($"Invalid float value: '{token.Text}'", token);
      return null; 
    }

    public override object ConvertInputValue(object value) {
      switch(value) {
        case null: return null;
        case Single s: return s;
        case double d: return (float) d;
        case int i: return (float)i;
        case long lng: return (float)lng;
        case decimal dec: return (float)dec; 
        default:
          throw new Exception($"Invalid Float value: '{value}'");
      }
    }
  }
}
