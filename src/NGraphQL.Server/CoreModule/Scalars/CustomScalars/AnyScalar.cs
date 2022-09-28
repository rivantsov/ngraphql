using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  /// <summary>Represents an untyped object. </summary>
  public class AnyScalar: CustomScalar {
    // refs to scalar instances 
    MapScalar _map;
    StringScalar _string;
    LongScalar _long;
    DoubleScalar _double;
    DateScalar _date;
    UuidScalar _uuid; 

    public AnyScalar(): base("Any", "'Any' scalar (untyped object) ", typeof(object)) {
      base.CanHaveSelectionSubset = true;
    }

    public override void CompleteInit(GraphQLApiModel model) {
      base.CompleteInit(model);
      // get refs to other 'real' scalars
      _map = GetScalarInstance<MapScalar>();
      _string = GetScalarInstance<StringScalar>();
      _long = GetScalarInstance<LongScalar>();
      _double = GetScalarInstance<DoubleScalar>();
      _date = GetScalarInstance<DateScalar>();
      _uuid = GetScalarInstance<UuidScalar>();
    }

    public override string ToSchemaDocString(object value) {
      switch(value) {
        case null: return "null";
        case string s:
          return _string.ToSchemaDocString(value);
        case Int32 i:
        case long l:
          return _long.ToSchemaDocString(value);
        case Guid g:
          return _uuid.ToSchemaDocString(value);
        case DateTime dt:
          return _date.ToSchemaDocString(value);
        default:
          throw new Exception($"Any scalar error: cannot format (default) value: {value}.");
      }
    }

    public override object ConvertInputValue(RequestContext context, object value) {
      switch(value) {
        case JToken jtkn: 

      }
      return value; 
    }

    public override object ParseValue(RequestContext context, ValueSource valueSource) {
      switch(valueSource) {
        case TokenValueSource tvs: 

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

    // 
    private TScalar GetScalarInstance<TScalar>() where TScalar : Scalar {
      var stype = typeof(TScalar);
      foreach (var td in Model.Types)
        if (td is ScalarTypeDef stdef && stdef.Scalar.GetType() == stype)
          return (TScalar)stdef.Scalar;
      Model.Errors.Add($"AnyScalar init ERROR: failed to find scalar {stype}.");
      return default;
    }


  }
}
