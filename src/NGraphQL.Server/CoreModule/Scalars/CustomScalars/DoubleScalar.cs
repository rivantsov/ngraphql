﻿using System;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class DoubleScalar : CustomScalar {

    public DoubleScalar() : base("Double", "Double scalar", typeof(double)) {
      CanConvertFrom = new[] { typeof(Single), typeof(int), typeof(long), typeof(Decimal) }; 
    }

    public override object ParseToken(RequestContext context, TokenData token) {
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

    public override object ConvertInputValue(RequestContext context, object value) {
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
