using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {
  public class UuidTypeDef : ScalarTypeDef {

    public UuidTypeDef() : base("Uuid", typeof(Guid), isCustom: true) { }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr: //single quote string
          if(Guid.TryParse((string) tkn.ParsedValue, out var value))
            return value;
          break;
      }
      context.ThrowScalarInput($"Invalid Uuid value: '{tkn.Text}'", tokenInput);
      return null;
    }

    public override string FormatConstant(object value) {
      if(value == null)
        return "null";
      var g = (Guid)value;
      return "'" + g.ToString("D") + "'";
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case Guid g: return g;
        case string s:
          if (Guid.TryParse(s, out var g1))
            return g1;
          throw new Exception($"Failed to parse Uuid value."); //details will be added by exc handler
        default:
          throw new Exception($"Invalid Uuid value."); //details will be added by exc handler
      }
    }

  }
}
