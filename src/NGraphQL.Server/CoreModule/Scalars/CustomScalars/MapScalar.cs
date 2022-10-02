using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  /// <summary>Represents a Dictionary[string, object] type (Map in JavaScript). </summary>
  public class MapScalar: CustomScalar {

    public MapScalar(): base("Map", "Map (dictionary) scalar", typeof(Dictionary<string, object>)) {
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
        case JValue jv:
          if (jv.Type == JTokenType.Null)
            return null; 
          return jv.Value;
        case JObject jo:
          return jo.ToObject<Dictionary<string, object>>();
        default:
          throw new Exception($"Invalid value for type Dictionary<string, object>.");
      }
    }

    public override object ParseValue(RequestContext context, ValueSource valueSource) {
      switch(valueSource) {
        case ListValueSource lvs:
          return ParseFromList(context, lvs); 
        case ObjectValueSource ovs:
          return ParseFromObject(context, ovs);
        default:
          throw new InvalidInputException("Invalid input value for Map scalar; expected object or rank 2 array.", valueSource);
      }
    }

    public static object ParseFromList(RequestContext context, ListValueSource listVs) {
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

    public static object ParseFromObject(RequestContext context, ObjectValueSource objVs) {
      var dict = new Dictionary<string, object>();
      foreach(var fld in objVs.Fields) {
        var value = ParseEntryValue(context, fld.Value);
        dict[fld.Key] = value; 
      }
      return dict; 
    }

    public static object ParseEntryValue(RequestContext context, ValueSource vs) {
      switch(vs) {
        case TokenValueSource tvs:
          return tvs.TokenData.ParsedValue;
        case ListValueSource lvs:
        case ObjectValueSource ovs:
        default: 
          throw new InvalidInputException("Failed to parse input value; might be not supported by the Scalar.", vs);
      }
    }

  }
}
