using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Model.Request;
using NGraphQL.Server;
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
        case Dictionary<string, object> dict: 
          return dict;
        default:
          throw new Exception($"Invalid value for type Dictionary<string, object>.");
      }
    }

    public override object ParseValue(RequestContext context, ValueSource valueSource) {
      try {

      } catch(Exception ex) {

      }
      switch(valueSource) {
        case ListValueSource lvs:
          return ParseFromList(context, lvs); 
        case ObjectValueSource ovs:
          return ParseFromObject(context, ovs);
        default:
          throw new InvalidInputException("Invalid input value for Map scalar; expected object or rank 2 array.", valueSource);
      }
    }


    private object ParseFromList(RequestContext context, ListValueSource listVs) {
      var allOk = listVs.Values.All(vs => vs is ListValueSource lvs && lvs.Values.Length == 2 && 
         lvs.Values[0] is TokenValueSource keyVs && keyVs.TokenData.ParsedValue is string);
      if(!allOk)
        throw new InvalidInputException(
          $"Invalid Map (dict) literal. Expected array of 2-element arrays with key-value pairs.", listVs);
      // 
      var dict = new Dictionary<string, object>();
      foreach(var elemVs in listVs.Values) {
        var elemListVs = elemVs as ListValueSource;        
        var keyVs = elemListVs.Values[0] as TokenValueSource;
        var key = keyVs.TokenData.ParsedValue as string;
        var valueVs = ParseEntryValue(context, elemListVs.Values[1]);
        dict[key] = valueVs;         
      }
      return dict; 
    }

    private object ParseFromObject(RequestContext context, ObjectValueSource objVs) {
      var dict = new Dictionary<string, object>();
      foreach(var fld in objVs.Fields) {
        var value = ParseEntryValue(context, fld.Value);
        dict[fld.Key] = value; 
      }
      return dict; 
    }

    private object ParseEntryValue(RequestContext context, ValueSource vs) {
      switch(vs) {
        case TokenValueSource tvs:
          return tvs.TokenData.ParsedValue;
        case ListValueSource lvs:
        case ObjectValueSource ovs:
        default: 
          throw new InvalidInputException("Array and complex values not supported for Map values.", vs);
      }
    }

  }
}
