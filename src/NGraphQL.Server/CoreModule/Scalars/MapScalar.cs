using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  /// <summary>Represents a Dictionary[string, object] type (Map in JavaScript). </summary>
  public class MapScalar: Scalar {

    public MapScalar(): base("Map", "Map (dictionary) scalar", typeof(Dictionary<string, object>), isCustom: true) {
      base.CanHaveSelectionSubset = true; 
    }

    // 
    public override string ToSchemaDocString(object value) {
      return "(Error: map scalar may not have default value)";
    }

    public override object ConvertInputValue(RequestContext context, object value) {
      switch (value) {
        case Dictionary<string, object> dict: return dict;
        default:
          throw new Exception($"Invalid value for type Dictionary<string, object>.");
      }
    }

    public override object ParseValue(RequestContext context, ValueSource valueSource) {
      return base.ParseValue(context, valueSource);
    }

  }

}
