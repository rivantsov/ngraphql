﻿using System;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class DateTimeScalar : CustomScalar {

    public DateTimeScalar() : this("DateTime") {
      IsDefaultForClrType = true;
    }
    public DateTimeScalar(string name) : base(name, "DateTime scalar", typeof(DateTime)) { }

    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;

        case TermNames.StrSimple:
        case TermNames.Qstr:
          var vstr = (string) token.ParsedValue; //parser does all char escaping 
          if(DateTime.TryParse(vstr, out var value))
            return value;
          break;
      }
      context.ThrowScalarError($"Invalid datetime value: '{token.Text}'", token);
      return null; 
    }

    public override string ToSchemaDocString(object value) {
      if(value == null)
        return null;
      var dt = (DateTime)value; 
      if (dt.TimeOfDay == TimeSpan.Zero)
        return Quote(dt.ToString("yyyy-MM-dd"));
      return Quote(dt.ToString("s")); // sortable datetime format, ex: 2009-06-15T13:45:30
    }

    public override object ConvertInputValue(RequestContext context, object value) {
      switch (value) {
        case DateTime dt: return dt;
        case string s:
          if (DateTime.TryParse(s, out var d))
            return d;
          break;
      }
      throw new Exception($"Invalid DateTime value: '{value}'");
    }

    private string Quote(string v) {
      const string q = "'";
      return q + v + q; 
    }
  } //class
}
