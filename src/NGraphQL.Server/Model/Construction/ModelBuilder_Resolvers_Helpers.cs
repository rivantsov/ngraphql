using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Introspection;
using NGraphQL.Utilities;
using System.Linq.Expressions;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    private Func<object, object> CompileResolverExpression(ParameterExpression entityParam, Expression body) {

      // check if body is a MapTo func - return the source entity, mapping will be handled by the caller
      var methCall = body as MethodCallExpression;
      var usesFromMapFunc = methCall != null && methCall.Method.DeclaringType == typeof(GraphQLModule) &&
                             methCall.Method.Name == nameof(GraphQLModule.FromMap);
      if (usesFromMapFunc)
        body = methCall.Arguments[0];
      return ExpressionHelper.CompileResolverExpression(entityParam, body);
    }

    private bool VerifyFieldResolverMethod(FieldDef field, ResolverMethodInfo resolverMethod) {
      if (!VerifyResolverMethodReturnTypeCompatible(field, resolverMethod.Method))
        return false;
      if (!ValidateResolverMethodArguments(field, resolverMethod))
        return false;
      return !_model.HasErrors;
    }

    private bool VerifyResolverMethodReturnTypeCompatible(FieldDef field, MethodInfo method) {
      Type returnType = method.GetMemberReturnType(); 
      UnwrapClrType(returnType, method, out var retBaseType, out var kinds, null);
      var retTypeRank = kinds.GetListRank();
      var fldTypeRef = field.TypeRef;
      var fldTypeRank = fldTypeRef.Rank;
      if (field.TypeRef.TypeDef.IsEnumFlagArray())
        fldTypeRank--;
      if (retTypeRank != fldTypeRank) {
        AddError($"Resolver method {method.GetFullRef()}: return type {returnType.Name} (rank {retTypeRank}) is not compatible with type " +
                 $" {field.TypeRef.Name} of  field '{field.Name}'; list rank mismatch.");
        return false;
      }
      var withBaseType = fldTypeRef.TypeDef.ClrType;
      switch (fldTypeRef.TypeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          if (retBaseType != withBaseType) {
            AddError($"Resolver method {method.GetFullRef()}: return type is incompatible with type {fldTypeRef.Name} of  field '{field.Name}'.");
            return false;
          }
          return true;

        case ObjectTypeDef objTypeDef:
          var mapping = objTypeDef.Mappings.Find(m => m.EntityType == retBaseType);
          if (mapping == null) {
            AddError($"Resolver method '{method.GetFullRef()}' return type '{retBaseType}' is not mapped to field type {fldTypeRef.Name}");
            return false;
          }
          return true;

        case UnionTypeDef _:
        case InterfaceTypeDef _:
          //TODO: maybe implement later
          return true;
      }
      return true;
    }

    private bool ValidateResolverMethodArguments(FieldDef fieldDef, ResolverMethodInfo resolverMethod) {
      var resMethod = resolverMethod.Method; 
      // Check first parameter - must be IFieldContext
      var prms = resMethod.GetParameters();
      if (prms.Length == 0 || prms[0].ParameterType != typeof(IFieldContext)) {
        AddError($"Resolver method {resMethod.GetFullRef()}: the first parameter must be of type '{nameof(IFieldContext)}'.");
        return false;
      }
      // compare list of field parameters with list of resolver method parameters; 
      //  resolver method has extra FieldContext and Parent parameters
      var argCountDiff = 1;
      if (!fieldDef.Flags.IsSet(FieldFlags.Static))
        argCountDiff = 2;
      var expectedPrmCount = fieldDef.Args.Count + argCountDiff;
      if (expectedPrmCount != prms.Length) {
        AddError($"Resolver method {resMethod.GetFullRef()}: parameter count mismatch with field arguments, expected {expectedPrmCount}, " + 
           "with added IFieldContext and possibly Parent object parameter. ");
        return false; 
      }
      // parameter names/types must be identical
      for(int i = argCountDiff; i < prms.Length; i++) {
        var prm = prms[i];
        var arg = fieldDef.Args[i - argCountDiff];
        if (prm.Name != arg.Name || prm.ParameterType != arg.ParamType) {
          AddError($"Resolver method {resMethod.GetFullRef()}: parameter name/type mismatch with field argument; parameter: {prm.Name}.");
          return false; 
        }
      }
      return true;
    }

    private void VerifyListParameterType(Type type, MethodBase method, string paramName) {
      if (!type.IsArray && !type.IsInterface)
        AddError($"Method {method.GetFullRef()}: Invalid list parameter type - must be array or IList<T>; parameter {paramName}. ");
    }

    private void VerifyAllResolversAssigned(ObjectTypeMapping mapping) {
      foreach (var fres in mapping.FieldResolvers) {
        // so far we have only exec type to set, or post error
        if (fres.ResolverFunc == null && fres.ResolverMethod == null) {
          var fldName = fres.Field.Name;
          var typeDef = mapping.TypeDef;          
          var fldRef = $"'{typeDef.ClrType.Name}.{fldName}', mapping from (entity) type '{mapping.EntityType}', " +
                       $" (module '{typeDef.Module.Name}')";
          AddError($"Field '{fldName}' has no associated resolver or mapped entity field. Field: {fldRef}.");
        }
      } // foreach fres
    }

    private IList<ResolverMethodInfo> FindResolvers(string name, Type resType = null) {
      var list = _allResolverMethods
        .Where(ri => ri.Method.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        .Where(ri => resType == null || ri.ResolverClass.Type == resType)
        .ToList();
      return list; 
    }
  }
}
