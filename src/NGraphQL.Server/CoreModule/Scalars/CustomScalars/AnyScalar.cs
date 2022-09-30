using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    DateTimeScalar _date;
    UuidScalar _uuid;

    public AnyScalar(): base("Any", "'Any' scalar (untyped object) ", typeof(object)) {
      base.CanHaveSelectionSubset = true;
    }

    public override void CompleteInit(GraphQLApiModel model) {
      base.CompleteInit(model);
      // get refs to other 'real' scalars
      _map = GetScalar<MapScalar>();
      _string = GetScalar<StringScalar>();
      _long = GetScalar<LongScalar>();
      _double = GetScalar<DoubleScalar>();
      _date = GetScalar<DateTimeScalar>();
      _uuid = GetScalar<UuidScalar>();
    }

    public override string ToSchemaDocString(object value) {
      switch(value) {
        case null: return "null";
        case string _:
          return _string.ToSchemaDocString(value);
        case Int32 _:
        case long _:
          return _long.ToSchemaDocString(value);
        case double _:
          return _double.ToSchemaDocString(value);
        case Guid _:
          return _uuid.ToSchemaDocString(value);
        case DateTime dt:
          return _date.ToSchemaDocString(value);
        default:
          throw new Exception($"Any scalar error: cannot format value: {value}.");
      }
    }

    public override object ConvertInputValue(RequestContext context, object value) {
      if (value == null)
        return null;
      var type = value.GetType();
      if (type.IsPrimitive)  // bool, float, double, all int types, char
        return value;
      var tname = value.GetType().Name;
      if (type.Name == "JObject" || type.Name == "JToken")
        return value; //return as raw JObject
      throw new Exception($"Not handled case for value type {type} in AnyScalar.ConvertInputValue.");
    }

    public override object ParseValue(RequestContext context, ValueSource valueSource) {
      switch(valueSource) {
        case TokenValueSource tvs:
          return tvs.TokenData.ParsedValue;
        case ListValueSource lvs:
          return MapScalar.ParseFromList(context, lvs); 
        case ObjectValueSource ovs:
          return MapScalar.ParseFromObject(context, ovs);
        default:
          throw new InvalidInputException("'Any' Scalar error: failed to parse value.", valueSource);
      }
    }

    List<Scalar> _allScalars;

    private TScalar GetScalar<TScalar>() where TScalar : Scalar {
      _allScalars ??= Model.Types.OfType<ScalarTypeDef>().Select(td => td.Scalar).ToList();
      var scalar = _allScalars.FirstOrDefault(s => s is TScalar);
      if (scalar != null)
        return (TScalar)scalar; 
      Model.Errors.Add($"AnyScalar init ERROR: failed to find scalar {typeof(TScalar)}.");
      return default;
    }


  }
}
