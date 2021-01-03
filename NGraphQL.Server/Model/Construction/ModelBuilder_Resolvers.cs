using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    private bool MapFieldResolvers() {
      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      var typesToMap = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
      // First map by ResolvesField attribute on resolver methods
      MapResolversByResolvesFieldAttribute(typesToMap);
      // Next try to map by Resolver attribute on field
      MapResolversByResolverAttribute(typesToMap); 

      return !_model.HasErrors;
    }

    // map resolvers having [ResolvesField] attribute
    private void MapResolversByResolvesFieldAttribute(IList<ObjectTypeDef> typesToMap) {
      // go thru resolver classes, find methods with ResolvesField attr
      foreach(var resClass in _model.Resolvers) {
        var resMethods = resClass.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach(var resMeth in resMethods) {
          var resAttr = resMeth.GetAttribute<ResolvesFieldAttribute>();
          if (resAttr == null)
            continue; 
          // check target type
          if (resAttr.TargetType != null) {
            if(!_model.TypesByClrType.TryGetValue(resAttr.TargetType, out var typeDef) || !(typeDef is ObjectTypeDef objTypeDef)) {
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target type '{resAttr.TargetType}' not registered or "
                       + "is not Object type.");
              continue; 
            }
            // match field
            var fld = objTypeDef.Fields.FirstOrDefault(f => f.Name == resAttr.FieldName);
            if (fld == null) {
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target field '{resAttr.FieldName}' not found "
                       + $"on type '{resAttr.TargetType}'.");
              continue;
            }
            SetupFieldResolverMethod(objTypeDef, fld, resMeth, resAttr);
            continue; 
          } // if TargetType != null
          // TargetType is null - find match by name only
          var fields = typesToMap.SelectMany(t => t.Fields).Where(f => f.Name == resAttr.FieldName).ToList(); 
          switch(fields.Count) {
            case 1:
              var fld = fields[0];
              SetupFieldResolverMethod((ObjectTypeDef) fld.OwnerType, fld, resMeth, resAttr);
              break;

            case 0:
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target field '{resAttr.FieldName}' not found "
                       + $"on any object type.");
              break;
            
            default:
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': multipe target fields '{resAttr.FieldName}' "
                       + $"found on Object types.");
              break; 
          }
        } //foreach resMeth
      } //foreach resClass
    }

    private bool MapResolversByResolverAttribute(IList<ObjectTypeDef> typesToMap) {
      foreach (var typeDef in typesToMap) {
        foreach (var field in typeDef.Fields) {
          var resAttr = GetAllAttributes(field.ClrMember).Find<ResolverAttribute>();
          if (resAttr == null)
            continue; 
          var resolverType = resAttr.ResolverClass;
          if (resolverType != null) {
            if (!typeDef.Module.ResolverTypes.Contains(resolverType)) {
              AddError($"Field {typeDef.Name}.{field.Name}: target resolver class {resolverType.Name} is not registered with module. ");
              return false;
            }
          }
          // 
          var methName = resAttr.MethodName ?? field.ClrMember.Name;
          List<MethodInfo> methods;
          if (resolverType != null) {
            methods = resolverType.GetResolverMethods(methName);
            // with explicit resolver, if method not found - it is error
            if (methods.Count == 0) {
              AddError($"Field {typeDef.Name}.{field.Name}: failed to match resolver method; target resolver class {resolverType.Name}.");
              return false;
            }
          } else {
            // targetResolver is null
            methods = new List<MethodInfo>();
            foreach (var resType in typeDef.Module.ResolverTypes) {
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
        } //foreach field
      } //foreach typeDef
      return !_model.HasErrors;
    }//method

    private void MapObjectFieldsResolversAndExpressions(IList<ObjectTypeDef> typesToMap) {
      foreach (var objTypeDef in typesToMap) {
        // process explicit mapping expressions
        if (objTypeDef.Mapping?.Expression != null) {
          ProcessEntityMappingExpression(objTypeDef);
        }
        //Try find resolver
        foreach (var fld in objTypeDef.Fields) {
          TryFindAssignFieldResolver(objTypeDef, fld);
        }
        // finally, if field is not mapped, but there's mapping for Object, try mapping by matching name
        if (objTypeDef.Mapping != null)
          ProcessMappingForMatchingMembers(objTypeDef);
      }
    }

    private bool SetupFieldResolverMethod(ObjectTypeDef typeDef, FieldDef field, MethodInfo resolverMethod, Attribute sourceAttr) {
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

      field.Resolver = new ResolverMethodInfo() { SourceAttribute = sourceAttr, Method = resolverMethod, 
            ResolverClass = resolverMethod.DeclaringType,
          ReturnsTask = returnsTask, TaskResultReader = taskResultReader };
      if (returnsTask)
        field.Flags |= FieldFlags.ReturnsTask;
      if (typeDef is ObjectTypeDef otd && otd.TypeRole == ObjectTypeRole.Data)
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
  }
}
