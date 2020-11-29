using System;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class BooleanScalar : Scalar {

    public BooleanScalar() : base("Boolean", null, typeof(bool)) { }

    public override object ParseToken(IScalarContext context, TokenData tokenData) {
      switch(tokenData.TermName) {
        case TermNames.NullValue:
          return null;
        case TermNames.True:
          return true;
        case TermNames.False:
          return false;
      }
      throw new ScalarException($"Invalid value: '{tokenData.Text}' for boolean type.", tokenData);
    }

    public override object ConvertInputValue(object value) {
      switch(value) {
        case bool b: return b; 
        default:
          throw new Exception($"Invalid boolean value: '{value}'");
      }
    }
  }
}
