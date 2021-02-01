using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using NGraphQL.Core.Scalars;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server.AspNetCore {

  public class JsonVariablesDeserializer {

    public JsonVariablesDeserializer() {
    }

    public void PrepareRequestVariables(RequestContext context) {
      var req = context.RawRequest;
      if (req.Variables == null || req.Variables.Count == 0)
        return;
      context.RawVariables = req.Variables; //save original copy
      req.Variables = new Dictionary<string, object>(); //replace with new one
      var op = context.Operation;
      var path = new List<object>(); // it must be list, not array! items might be added in ReadValue call
      foreach (var varDef in op.Variables) {
        if (!context.RawVariables.TryGetValue(varDef.Name, out var rawValue))
          continue; //it might have default, or might be nullable; if not, it will be handled later
        path.Add(varDef.Name); 
        var clrValue = ReadValue(context, varDef.InputDef.TypeRef, rawValue, path);
        if (clrValue == null && varDef.InputDef.TypeRef.Kind == TypeKind.NonNull)
          AddError(context, $"Variable {varDef.Name}: null value not allowed.", path);
        req.Variables[varDef.Name] = clrValue;
        path.Clear();
      }
    }

    private object ReadValue(RequestContext context, TypeRef typeRef, object jsonValue, IList<object> path) {
      if (jsonValue == null)
        return null;
      switch (typeRef.Kind) {

        case TypeKind.NonNull:
          var result = ReadValue(context, typeRef.Inner, jsonValue, path);
          if (result == null)
            AddError(context, $"Null value not allowed.", path);
          return result; 

        case TypeKind.List:
          if (typeRef.TypeDef.IsEnumFlagArray() && typeRef.Rank == 1)
            return ReadEnum(context, (EnumTypeDef) typeRef.TypeDef, jsonValue, path);
          else 
            return ReadList(context, typeRef, jsonValue, path);

        case TypeKind.Scalar:
          var scalarDef = (ScalarTypeDef)typeRef.TypeDef;
          return ReadScalar(context, scalarDef.Scalar, jsonValue, path);
          
        case TypeKind.Enum:
          return ReadEnum(context, (EnumTypeDef) typeRef.TypeDef, jsonValue, path);

        case TypeKind.InputObject:
          return ReadInputObject(context, (InputObjectTypeDef) typeRef.TypeDef, jsonValue, path);

        default:
          AddError(context, $"Invalid input value type: only scalar, enum or input object are allowed.", path);
          return null; 
      }
    }

    private object ReadList(RequestContext context, TypeRef typeRef, object jsonValue, IList<object> path) {
      var elemTypeRef = typeRef.Inner;
      var elemClrType = elemTypeRef.ClrType;
      switch(jsonValue) {
        case JArray jArr:
          var jItems = jArr.ToList();
          path.Add(0); //reserve extra slot
          var resArr = Array.CreateInstance(elemClrType, jArr.Count);
          for (int i = 0; i < jItems.Count; i++) {
            path[path.Count - 1] = i;
            var item = ReadValue(context, elemTypeRef, jItems[i], path);
            resArr.SetValue(item, i); 
          }
          path.RemoveAt(path.Count - 1);
          return resArr; 
      }
      // if we are here, then we have a single value; process it and then wrap into array[1] (coerce rule)
      var v1 = ReadValue(context, elemTypeRef, jsonValue, path);
      var arr1 = Array.CreateInstance(elemClrType, 1);
      arr1.SetValue(v1, 0);
      return arr1;
    }

    private object ReadScalar(RequestContext context, Scalar scalar, object jsonValue, IList<object> path) {
      var value = UnwrapSimpleValue(context, jsonValue, path);
      if (value == null)
        return null; 
      var res = scalar.ConvertInputValue(context, value);
      return res; 
    }

    private object UnwrapSimpleValue(RequestContext context, object value, IList<object> path) {
      switch (value) {
        case null: return null;
        case long lng:
          // Json deserializer reads all ints as long; that brings some inconveniences down the road, so we convert small values back to int
          if (lng <= int.MaxValue && lng >= int.MinValue)
            return (int)lng;
          else
            return value;
        case JToken jtoken:
          switch (jtoken.Type) {
            case JTokenType.String:
              return jtoken.Value<string>();
            case JTokenType.Integer:
              return jtoken.Value<int>();
            case JTokenType.Float:
              return jtoken.Value<double>();
            case JTokenType.Boolean:
              return jtoken.Value<bool>();
            default:
              return jtoken.Value<string>(); //let Scalar parse it
          } // switch jtoken.type
        default:
          return value;
      }//switch value
    }

    private object ReadEnum(RequestContext context, EnumTypeDef enumTypeDef, object jsonValue, IList<object> path) {
      var handler = enumTypeDef.Handler;
      var src = path.FirstOrDefault()?.ToString();
      switch (jsonValue) {
        case JArray jArr:
          if (handler.IsFlagSet) {
            var strArr = jArr.Select(v => v.ToString()).ToArray();
            var enumV = handler.ConvertStringListToFlagsEnumValue(strArr);
            return enumV;
          } else {
            AddError(context, $"Enum {handler.EnumName} is not flag set, array input is invalid; around '{src}'.", path);
            return handler.NoneValue;
          }

        case JValue jv:
          if (jv.Value == null)
            return null;
          var enumVs = handler.ConvertStringToEnumValue(jv.Value.ToString());
          return enumVs;

        case string sV:
          var enumV1 = handler.ConvertStringToEnumValue(sV);
          return enumV1;

        default:
          AddError(context, $"Invalid value for enum {handler.EnumName}, around {src}.", path);
          return handler.NoneValue;
      } //switch
    }
    
    private object ReadInputObject(RequestContext context, InputObjectTypeDef inputTypeDef, object jsonValue, IList<object> path) {
      var jObj = jsonValue as JObject; 
      if (jObj == null) {
        AddError(context, "Expected Json object segment.", path);
      }
      var clrType = inputTypeDef.ClrType;
      var missingFldNamess = inputTypeDef.GetRequiredFields();
      var inputObj = Activator.CreateInstance(clrType);
      foreach (var prop in jObj.Properties()) {
        path.Add(prop.Name); 
        var fldDef = inputTypeDef.Fields.FirstOrDefault(f => f.Name == prop.Name);
        if (fldDef == null) {
          AddError(context, $"Field {prop.Name} not defined on input object {inputTypeDef.Name}.", path);
          continue;
        }
        if (missingFldNamess.Contains(prop.Name))
          missingFldNamess.Remove(prop.Name); 
        
        var value = ReadValue(context, fldDef.TypeRef, prop.Value, path);
        if (value == null && fldDef.TypeRef.IsNotNull) {
          AddError(context, $"Field {prop.Name} on input object {inputTypeDef.Name} may not be null.", path);
          continue; 
        }
        fldDef.InputObjectClrMember.SetMember(inputObj, value);
        path.RemoveAt(path.Count - 1);
      }
      // check that all required fields are provided
      if (missingFldNamess.Count > 0) {
        var missingStr = string.Join(", ", missingFldNamess);
        AddError(context, $"Input object {inputTypeDef.Name}: missing required fields: {missingStr}", path); 
      }

      return inputObj; 
    }

    private void AddError(RequestContext context, string message, IList<object> path) {
      var err = new GraphQLError() {
        Message = "Variables: " + message,
        Path = path
      };
      context.AddError(err);
    }
  }
}
