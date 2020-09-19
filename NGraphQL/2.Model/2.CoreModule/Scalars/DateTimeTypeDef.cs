using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {

  public class DateTimeTypeDef : ScalarTypeDef {

    public DateTimeTypeDef(string name = "DateTime") : base(name, typeof(DateTime), isCustom: true) { }

    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr:
          var vstr = (string) tkn.ParsedValue; //parser does all char escaping 
          if(DateTime.TryParse(vstr, out var value))
            return value;
          break;
      }
      context.ThrowScalarInput($"Invalid datetime value: '{tkn.ParsedValue}'", tokenInput);
      return null; 
    }

    public override string FormatConstant(object value) {
      if(value == null)
        return null;
      var dt = (DateTime)value; 
      if (dt.TimeOfDay == TimeSpan.Zero) {
        return dt.ToString("yyyy-MM-dd");
      }
      return dt.ToString("s"); // sortable datetime format, ex: 2009-06-15T13:45:30
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case DateTime dt: return dt;
        case string s:
          if (DateTime.TryParse(s, out var d))
            return d;
          throw new Exception($"Invalid DateTime value: '{value}'"); 
        default:
          throw new Exception($"Invalid DateTime value: '{value}'");
      }
    }


  }

}
