using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Core {

  public class FloatTypeDef : ScalarTypeDef {

    public FloatTypeDef() : base("Float", typeof(Single)) {
      CanConvertFrom = new[] { typeof(Double), typeof(int), typeof(long), typeof(Decimal) };
    }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          if(double.TryParse(tkn.Text, out var value))
            return value;
          break;
      }
      context.ThrowScalarInput($"Invalid float value: '{tkn.Text}'", tokenInput);
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
