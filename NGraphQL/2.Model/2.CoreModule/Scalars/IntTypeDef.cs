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

  public class IntTypeDef : ScalarTypeDef {

    public IntTypeDef() : base("Int", typeof(int)) {
      CanConvertFrom = new[] { typeof(long) };
    }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          return tkn.ParsedValue;  //relying on converting by parser, including hex conversion
      }
      context.ThrowScalarInput($"Invalid int value: '{tkn.Text}'", tokenInput);
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

        case bool b:
        default:
          throw new Exception($"Invalid Int value: '{value}'"); 
      }
    }
  }
}
