using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NGraphQL.Core.Scalars;
using NGraphQL.Introspection;
using NGraphQL.Json;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server.AspNetCore {

  public class JsonVariablesDeserializer {
    JsonSerializerOptions _jsonOptions = JsonDefaults.JsonOptions;

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
        object clrValue;
        if (rawValue is JsonElement jsonElem) {
          clrValue = jsonElem.Deserialize(varDef.InputDef.TypeRef.ClrType, _jsonOptions); 
        } else {
          clrValue = rawValue;
        }
        if (clrValue == null && varDef.InputDef.TypeRef.Kind == TypeKind.NonNull)
          AddError(context, $"Variable {varDef.Name}: null value not allowed.", path);
        req.Variables[varDef.Name] = clrValue;
        path.Clear();
      }
    }

    private void AddError(RequestContext context, string message, IList<object> path) {
      var err = new GraphQLError("Variables: " + message, path);
      context.AddError(err);
    }
  }
}
