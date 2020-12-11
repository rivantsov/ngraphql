using System;

using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class TimeScalar : Scalar {

    public TimeScalar() : base("Time", "Time scalar", typeof(TimeSpan), isCustom: true) { }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr:
          var vstr = (string)token.ParsedValue; 
          if(TimeSpan.TryParse(vstr, out var value))
            return value;
          break;
      }
      context.ThrowScalarError($"Invalid time value: '{token.ParsedValue}'", token);
      return null; 
    }

    public override string ToSchemaDocString(object value) {
      if(value == null)
        return null;
      var dt = (TimeSpan)value; 
      return dt.ToString("c"); 
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case TimeSpan ts: return ts;
        case string s:
          if (TimeSpan.TryParse(s, out var t))
            return t;
          throw new Exception($"Invalid Time value: '{value}'");
        default:
          throw new Exception($"Invalid Time value: '{value}'");
      }
    }

  }

}
