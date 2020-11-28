using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model.Core {

  public class IdTypeDef : StringTypeDef {

    public IdTypeDef() : base("ID", isCustom: true) {
      IsDefaultForClrType = false; 
    }

    public override object ConvertInputValue(object value) {
      switch (value) {
        case string s: return s;
        default:
          throw new Exception($"Invalid ID value '{value}', expected string."); //details will be added by exc handler
      }
    }

  }
}
