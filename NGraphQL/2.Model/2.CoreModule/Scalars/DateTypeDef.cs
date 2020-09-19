using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model.Core {

  public class DateTypeDef: DateTimeTypeDef {

    public DateTypeDef(): base("Date") {
      IsDefaultForClrType = false; 
    }

    // 
    public override string FormatConstant(object value) {
      if(value is DateTime dt)
        return dt.Date.ToString("yyyy-MM-dd"); 
      return base.FormatConstant(value);
    }

    public override object ConvertInputValue(object value) {
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
