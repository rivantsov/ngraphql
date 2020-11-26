using System;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model.Core {

  public class TimeTypeDef : ScalarTypeDef {

    public TimeTypeDef() : base("Time", typeof(TimeSpan), isCustom: true) { }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr:
          var vstr = (string)tkn.ParsedValue; 
          if(TimeSpan.TryParse(vstr, out var value))
            return value;
          break;
      }
      context.ThrowScalarInput($"Invalid time value: '{tkn.ParsedValue}'", tokenInput);
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
