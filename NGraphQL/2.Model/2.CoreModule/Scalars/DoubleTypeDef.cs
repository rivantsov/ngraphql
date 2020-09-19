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

  public class DoubleTypeDef : ScalarTypeDef {

    public DoubleTypeDef() : base("Double", typeof(double), isCustom: true) {
      CanConvertFrom = new[] { typeof(Single), typeof(int), typeof(long), typeof(Decimal) }; 
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
      context.ThrowScalarInput($"Invalid double value: '{tkn.Text}'", tokenInput);
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
