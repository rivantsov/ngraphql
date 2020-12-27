using System;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {
  public class UuidScalar : Scalar {

    public UuidScalar() : base("Uuid", "Uuid scalar", typeof(Guid), isCustom: true) { }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr: //single quote string
          if(Guid.TryParse( (string) token.ParsedValue, out var value))
            return value;
          break;
      }
      context.ThrowScalarError($"Invalid Uuid value: '{token.Text}'", token);
      return null;
    }

    public override string ToSchemaDocString(object value) {
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
