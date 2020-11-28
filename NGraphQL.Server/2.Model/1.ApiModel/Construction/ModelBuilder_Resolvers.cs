using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NGraphQL.CodeFirst;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    private bool TryFindAssignFieldResolver(ObjectTypeDef typeDef, FieldDef field) {
      // check resolver
      var methName = field.ClrMember.Name;
      var resAttr = field.ClrMember.GetAttribute<ResolverAttribute>();
      if (resAttr != null)
        methName = resAttr.MethodName;
      List<MethodInfo> methods = null; 
      var targetResolver = resAttr?.ResolverClass;
      if (targetResolver != null) {
        if (!typeDef.Module.ResolverClasses.Contains(targetResolver)) {
          AddError($"Field {typeDef.Name}.{field.Name}: target resolver class {targetResolver.Name} is not registered with module. ");
          return false;
        }
        methods = targetResolver.GetResolverMethods(methName);
        // with explicit resolver, if method not found - it is error
        if (methods.Count == 0) {
          AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}, in class {targetResolver.Name}. ");
          return false;
        }
      } else {
        // targetResolver is null
        methods = new List<MethodInfo>();
        foreach (var resType in typeDef.Module.ResolverClasses) {
          var mlist = resType.GetResolverMethods(methName);
          methods.AddRange(mlist);  
        }
      }
      // if resolver not found
      switch (methods.Count) {
        case 0: 
          if (field.ClrMember.MemberType != MemberTypes.Method)
            return false; // if it is prop or field - it might have mapping; just return false
          // if field is method - it is error
          AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}. ");
          return false;
        
        case 1:
          return SetupFieldResolverMethod(typeDef, field, methods[0], resAttr);

        default:
          AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
          return false;
      }

    }//method

    private bool SetupFieldResolverMethod(ComplexTypeDef typeDef, FieldDef field, MethodInfo resolverMethod, ResolverAttribute resAttr) {
      var retType = resolverMethod.ReturnType;
      var returnsTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
      Func<object, object> taskResultReader = null;
      if (returnsTask) {
        retType = retType.GetGenericArguments()[0];
        taskResultReader = ReflectionHelper.CompileTaskResultReader(retType);
      }
      // validate return type
      if (!CheckReturnTypeCompatible(retType, field, resolverMethod))
        return false; 

      field.Resolver = new ResolverMethodInfo() { Attribute = resAttr, Method = resolverMethod, ResolverClass = resolverMethod.DeclaringType,
          ReturnsTask = returnsTask, TaskResultReader = taskResultReader };
      if (returnsTask)
        field.Flags |= FieldFlags.ReturnsTask;
      if (typeDef.TypeRole == TypeRole.DataType)
        field.Flags |= FieldFlags.HasParentArg; 
      ValidateResolverMethodArguments(typeDef, field); 
      return !_model.HasErrors;
    }

    private bool ValidateResolverMethodArguments(ComplexTypeDef typeDef, FieldDef fieldDef) {
      var resMethod = fieldDef.Resolver.Method; 
      // Check first parameter - must be IFieldContext
      var prms = resMethod.GetParameters();
      if (prms.Length == 0 || prms[0].ParameterType != typeof(IFieldContext)) {
        AddError($"Resolver method {resMethod.GetFullRef()}: the first parameter must be of type '{nameof(IFieldContext)}'.");
        return false;
      }

      // compare list of field parameters with list of resolver method parameters; 
      //  resolver method has extra FieldContext and Parent parameters
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

    private void VerifyListParameterType(Type type, MethodInfo method, string paramName) {
      if (!type.IsArray && !type.IsInterface)
        AddError($"Method {method.GetFullRef()}: Invalid list parameter type - must be array or IList<T>; parameter {paramName}. ");
    }

    private bool CheckReturnTypeCompatible(Type returnType, FieldDef field, MethodInfo method) {
      UnwrapClrType(returnType, method, out var retBaseType, out var kinds);
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
  }
}
