using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {

  public class DecimalTypeDef : ScalarTypeDef {

    public DecimalTypeDef() : base("Decimal", typeof(decimal)) {
      CanConvertFrom = new[] { typeof(Single), typeof(double), typeof(int), typeof(long) };
    }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          if(decimal.TryParse(tkn.Text, out var value))
            return value;
          break;
      }
      context.ThrowScalarInput($"Invalid int value: '{tkn.Text}'", tokenInput);
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
