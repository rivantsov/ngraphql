using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Core.Scalars;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Core {

  public class BooleanScalar : Scalar {

    public BooleanScalar() : base("Boolean", typeof(bool)) { }

    public override object ParseToken(IFieldContext context, TokenData tokenData) {
      switch(tokenData.TermName) {
        case TermNames.NullValue:
          return null;
        case TermNames.True:
          return true;
        case TermNames.False:
          return false;
      }
      context.ThrowScalarInput($"Invalid value: '{tokenData.Text}' for bool variable.", tokenData);
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
