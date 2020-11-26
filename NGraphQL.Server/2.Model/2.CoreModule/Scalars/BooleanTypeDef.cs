using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {

  public class BooleanTypeDef : ScalarTypeDef {

    public BooleanTypeDef() : base("Boolean", typeof(bool)) { }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;
        case TermNames.True:
          return true;
        case TermNames.False:
          return false;
      }
      context.ThrowScalarInput($"Invalid value: '{tkn.Text}' for bool variable.", tokenInput);
      return default(bool);
    }

    public override object ConvertInputValue(object value) {
      switch(value) {
        case bool b: return b; 
        default:
          throw new Exception($"Invalid bool value: '{value}'");
      }
    }
  }
}
