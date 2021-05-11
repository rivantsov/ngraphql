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

    private Func<object, object> CompileResolverExpression(FieldDef field, ParameterExpression entityParam, Expression body) {

      // check if body is a MapTo func - return the source entity, mapping will be handled by the caller
      var methCall = body as MethodCallExpression;
      var usesFromMapFunc = methCall != null && methCall.Method.DeclaringType == typeof(GraphQLModule) &&
                             methCall.Method.Name == nameof(GraphQLModule.FromMap);
      if (usesFromMapFunc)
        body = methCall.Arguments[0];
      else
        field.Flags |= FieldFlags.ResolverReturnsGraphQLObject;
      // TODO: add return type validation
      var baseLambda = Expression.Lambda(body, entityParam);
      var newParentPrm = Expression.Parameter(typeof(object));
      var parentObj = Expression.Convert(newParentPrm, entityParam.Type);
      var invokeBaseLambdaExpr = Expression.Invoke(baseLambda, parentObj);
      var convResultExpr = Expression.Convert(invokeBaseLambdaExpr, typeof(object));
      var newLambda = Expression.Lambda(convResultExpr, newParentPrm);
      var compiledLambda = newLambda.Compile();
      var func = (Func<object, object>)compiledLambda;
      return func;
    }

    private bool SetupFieldResolverMethod(ObjectTypeDef typeDef, FieldResolverInfo fieldRes, ResolverMethodInfo resolverInfo, Attribute sourceAttr) {
      var retType = resolverInfo.ReturnType;
      // validate return type
      if (!CheckReturnTypeCompatible(retType, fieldRes, resolverInfo.Method))
        return false;
        
      fieldRes.ResolverMethod = resolverInfo;
      if (resolverInfo.ReturnsTask)
        fieldRes.Flags |= FieldFlags.ResolverReturnsTask;
      if (typeDef is ObjectTypeDef otd && otd.TypeRole == TypeRole.Data)
        field.Flags |= FieldFlags.HasParentArg;
      field.ExecutionType = ResolverKind.Method;

      if (!ValidateResolverMethodArguments(typeDef, fieldRes))
        return false;
      return !_model.HasErrors;
    }

    private bool ValidateResolverMethodArguments(ComplexTypeDef typeDef, FieldResolverInfo fieldRes) {
      var resMethod = fieldRes.ResolverMethod.Method; 
      // Check first parameter - must be IFieldContext
      var prms = resMethod.GetParameters();
      if (prms.Length == 0 || prms[0].ParameterType != typeof(IFieldContext)) {
        AddError($"Resolver method {resMethod.GetFullRef()}: the first parameter must be of type '{nameof(IFieldContext)}'.");
        return false;
      }

      // compare list of field parameters with list of resolver method parameters; 
      //  resolver method has extra FieldContext and Parent parameters
      var fieldDef = fieldRes.Field; 
      var argCountDiff = 1;
      if (fieldDef.Flags.IsSet(FieldFlags.HasParentArg))
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

    private bool CheckReturnTypeCompatible(Type returnType, FieldDef field, MethodInfo method) {
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
          if(retBaseType != withBaseType) {
            AddError($"Resolver method {method.GetFullRef()}: return type is incompatible with type {fldTypeRef.Name} of  field '{field.Name}'.");
            return false; 
          }
          return true;

        case ObjectTypeDef objTypeDef:
          var mappedTypeDef = _model.GetMappedGraphQLType(retBaseType);
          if (mappedTypeDef != objTypeDef) {
            AddError($"Resolver method {method.GetFullRef()}: return type is incompatible with field type {fldTypeRef.Name}");
            return false;
          }
          return true;

        case UnionTypeDef _:
        case InterfaceTypeDef _:
          //TODO: implement later
          return true; 
      }
      return true;  
    }

    private void VerifyAllResolversAssigned() {
      foreach (var typeDef in _typesToAssignResolvers) {
        foreach (var mapping in typeDef.Mappings) {
          foreach (var fres in mapping.FieldResolvers) {
            // so far we have only exec type to set, or post error
            if (fres.ResolverFunc != null)
              fres.ResolverKind = ResolverKind.CompiledExpression;
            else if (fres.ResolverMethod != null)
              fres.ResolverKind = ResolverKind.Method;
            else {
              var fldName = fres.Field.Name;
              var fldRef = $"{typeDef.ClrType.Name}.{fldName}, mapping to {mapping.EntityType}, " +
                $" (module {typeDef.Module.Name})";
              AddError($"Field {fldName} has no associated resolver or mapped entity field. Field: {fldRef}");
            }
          } // foreach fres
        } // foreach mapping
      } // foreach typeDef
    }


  }
}
