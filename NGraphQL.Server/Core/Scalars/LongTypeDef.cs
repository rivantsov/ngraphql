using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {

  public class LongTypeDef : ScalarTypeDef {

    public LongTypeDef() : base("Long", typeof(long)) {
      CanConvertFrom = new[] { typeof(int) };
    }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.Number:
          return tkn.ParsedValue;  //relying on converting by parser, including hex conversion
      }
      context.ThrowScalarInput($"Invalid long value: '{tkn.Text}'", tokenInput);
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

        case bool b:
        default:
          throw new Exception($"Invalid Long value: '{value}'");
      }
    }

  }
}
