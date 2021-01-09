using System;

namespace NGraphQL.Core.Scalars {

  public class IdScalar : StringScalar {

    public IdScalar() : base("ID", "ID scalar", isCustom: true) {
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
