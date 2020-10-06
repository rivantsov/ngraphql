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
    Dictionary<string, List<MethodInfo>> _resolverMethodsByName;

    private bool CollectResolverMethods() {
      var allMethods = new List<MethodInfo>();
      foreach (var module in _api.Modules) {
        var mName = module.GetType().Name;
        foreach (var resType in module.ResolverClasses) {
          if (!resType.IsClass) {
            AddError($"Type {resType} may not be registered as resolver class; module: {mName}. ");
            continue;
          }
          _model.Resolvers.Add(new ResolverClassInfo() { Module = module, Type = resType });
          // collect all methods
          var methods = resType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
          allMethods.AddRange(methods); 
        }
      }
      _resolverMethodsByName = allMethods.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.ToList());
      return !_model.HasErrors;
    }

    private bool TryFindAssignFieldResolver(ObjectTypeDef typeDef, FieldDef field) {
      // check resolver
      var methName = field.ClrMember.Name;
      var resAttr = field.ClrMember.GetAttribute<ResolverAttribute>();
      if (resAttr != null) {
        methName = resAttr.MethodName;
      }
      MethodInfo resMethod;
      if (!_resolverMethodsByName.TryGetValue(methName, out var resMethods))
        return false;
      var resClass = resAttr?.ResolverClass;
      // Resolver method(s) found
      if (resMethods.Count == 1) {
        resMethod = resMethods[0];
        if (resClass != null && resMethod.DeclaringType != resClass) { 
          AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}, in class {resClass.Name}. ");
          return false; 
        }
      } else {
        // we have more than 1 method, use explicit class type in Resolver attribute
        if (resClass == null) {
          var resTypeNames = string.Join(", ", resMethods.Select(m => m.DeclaringType.Name));
          AddError($"Found more than one resolver method ({methName}) for field '{field.Name}' " +
            $" in resolver classes: [{resTypeNames}]; use Resolver attribute to specify the resolver class explicitly.");
          return false;
        }
        resMethod = resMethods.FirstOrDefault(m => m.DeclaringType == resClass);
        if (resMethod == null) {
          AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}, in class {resClass.Name}. ");
          return false;
        }
      } //else

      // Found matching resolver method
      if (!SetupResolverMethod(field, resMethod, resAttr))
        return false;
      return true; 
    }//method

    private bool SetupResolverMethod(FieldDef field, MethodInfo resolverMethod, ResolverAttribute resAttr) {
      var retType = resolverMethod.ReturnType;
      var returnsTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
      Func<object, object> taskResultReader = null;
      if (returnsTask) {
        retType = retType.GetGenericArguments()[0];
        taskResultReader = ReflectionHelper.CompileTaskResultReader(retType);
      }
      // validate return type
      if (!CheckReturnTypeCompatible(retType, field.TypeRef, resolverMethod))
        return false; 

      field.Resolver = new ResolverMethodInfo() { Attribute = resAttr, Method = resolverMethod, ReturnsTask = returnsTask, TaskResultReader = taskResultReader };
      if (returnsTask)
        field.Flags |= FieldFlags.ReturnsTask;
      return !_model.HasErrors;
    }

    private bool CheckReturnTypeCompatible(Type returnType, TypeRef withTypeRef, MethodInfo method) {
      UnwrapClrType(returnType, method, out var retBaseType, out var kinds);
      var retTypeRank = kinds.GetListRank(); 
      if (retTypeRank != withTypeRef.Rank) {
        AddError($"Resolver method {method.GetFullRef()}: return type {returnType.Name} (rank {retTypeRank}) is not compatible with field type " + 
                 $" {withTypeRef.Name} (rank {withTypeRef.Rank}); list rank mismatch.");
        return false; 
      }
      var withBaseType = withTypeRef.TypeDef.ClrType; 
      switch (withTypeRef.TypeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          if(retBaseType != withBaseType) {
            AddError($"Resolver method {method.GetFullRef()}: return type is incompatible with field type {withTypeRef.Name}");
            return false; 
          }
          return true;

        case ObjectTypeDef objTypeDef:
          var mappedTypeDef = _model.GetMappedGraphQLType(retBaseType);
          if (mappedTypeDef != objTypeDef) {
            AddError($"Resolver method {method.GetFullRef()}: return type is incompatible with field type {withTypeRef.Name}");
            return false;
          }
          return true;

        case UnionTypeDef utd: 
      }
      throw new NotImplementedException(); 
    }
  }
}
