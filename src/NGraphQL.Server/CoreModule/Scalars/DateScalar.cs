using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  public class DateScalar: Scalar {

    public DateScalar(): base("Date", "Date scalar", typeof(DateTime), isCustom: true) {
      IsDefaultForClrType = false; 
    }

    // 
    public override string ToSchemaDocString(object value) {
      if(value is DateTime dt)
        return dt.Date.ToString("yyyy-MM-dd"); 
      return base.ToSchemaDocString(value);
    }

    public override object ConvertInputValue(RequestContext context, object value) {
      switch (value) {
        case DateTime dt: return dt.Date;
        case string s:
          if (DateTime.TryParse(s, out var d))
            return d.Date;
          throw new Exception($"Invalid Date value: '{value}'");
        default:
          throw new Exception($"Invalid Date value: '{value}'");
      }
    }

  }

}
