using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {
    IList<ObjectTypeDef> _typesToMapFields; 

    private bool MapObjectFields() {
      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      _typesToMapFields = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
      // 1. Entity mapping expressions
      ProcessEntityMappingExpressions();
      if (_model.HasErrors) return false; 

      // 2. Map by ResolvesField attribute on resolver methods
      MapResolversByResolvesFieldAttribute();
      if (_model.HasErrors) return false;

      // 3. to map by Resolver attribute on field
      MapFieldsToResolversByResolverAttribute();
      if (_model.HasErrors) return false;

      // 4. Default mapping to entities by name
      ProcessMappingForMatchingMembers();
      if (_model.HasErrors) return false;

      // 5. Map to resolvers by name 
      MapFieldsToResolversByName();
      if (_model.HasErrors) return false;

      // 6. Verify all assigned
      VerifyFieldMappings(); 

      return !_model.HasErrors;
    }

    private void VerifyFieldMappings() {
      foreach (var typeDef in _typesToMapFields) {
        foreach (var field in typeDef.Fields) {
          // so far we have only exec type to set, or post error
          if (field.Reader != null)
            field.ExecutionType = FieldExecutionType.Reader;
          else if (field.Resolver != null)
            field.ExecutionType = FieldExecutionType.Resolver;
          else
            AddError($"Field '{typeDef.ClrType.Name}.{field.Name}' (module {typeDef.Module.Name}) has no associated resolver or mapped entity field.");
        }
      }
    }

    // map resolvers having [ResolvesField] attribute
    private void MapResolversByResolvesFieldAttribute() {
      // go thru resolver classes, find methods with ResolvesField attr
      foreach(var resClass in _model.Resolvers) {
        var resMethods = resClass.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach(var resMeth in resMethods) {
          var resAttr = resMeth.GetAttribute<ResolvesFieldAttribute>();
          if (resAttr == null)
            continue;
          var fieldName = resAttr.FieldName.FirstLower();
          // check target type
          if (resAttr.TargetType != null) {
            if(!_model.TypesByClrType.TryGetValue(resAttr.TargetType, out var typeDef) || !(typeDef is ObjectTypeDef objTypeDef)) {
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target type '{resAttr.TargetType}' not registered or "
                       + "is not Object type.");
              continue; 
            }
            // match field
            var fld = objTypeDef.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (fld == null) {
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target field '{fieldName}' not found "
                       + $"on type '{resAttr.TargetType}'.");
              continue;
            }
            SetupFieldResolverMethod(objTypeDef, fld, resMeth, resAttr);
            continue; 
          } // if TargetType != null
          // TargetType is null - find match by name only
          var fields = _typesToMapFields.SelectMany(t => t.Fields).Where(f => f.Name == fieldName).ToList(); 
          switch(fields.Count) {
            case 1:
              var fld = fields[0];
              SetupFieldResolverMethod((ObjectTypeDef) fld.OwnerType, fld, resMeth, resAttr);
              break;

            case 0:
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': target field '{fieldName}' not found "
                       + $"on any object type.");
              break;
            
            default:
              AddError($"Resolver method '{resClass.Type}.{resMeth.Name}': multipe target fields '{fieldName}' "
                       + $"found on Object types.");
              break; 
          }
        } //foreach resMeth
      } //foreach resClass
    }

    private void MapFieldsToResolversByResolverAttribute() {
      foreach (var typeDef in _typesToMapFields) {
        foreach (var field in typeDef.Fields) {
          if (field.ClrMember == null)
            continue; //__typename has no clr member
          var resAttr = GetAllAttributes(field.ClrMember).Find<ResolverAttribute>();
          if (resAttr == null)
            continue; 
          var resolverType = resAttr.ResolverClass;
          if (resolverType != null) {
            if (!typeDef.Module.ResolverTypes.Contains(resolverType)) {
              AddError($"Field {typeDef.Name}.{field.Name}: target resolver class {resolverType.Name} is not registered with module. ");
              continue;
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
              continue;
            }
          } else {
            // targetResolver is null
            methods = new List<MethodInfo>();
            foreach (var resType in typeDef.Module.ResolverTypes) {
              var mlist = resType.GetResolverMethods(methName);
              methods.AddRange(mlist);
            }
          }
          switch (methods.Count) {
            case 0:
              AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}. ");
              break;

            case 1:
              SetupFieldResolverMethod(typeDef, field, methods[0], resAttr);
              break;

            default:
              AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
              break; 
          }
        } //foreach field
      } //foreach typeDef
    }//method

    private void MapFieldsToResolversByName() {
      foreach (var typeDef in _typesToMapFields) {
        // get all resolver methods from the same module
        var allMethods = new List<MethodInfo>();
        foreach (var resType in typeDef.Module.ResolverTypes) {
          var methods = resType.GetMethods();
          allMethods.AddRange(methods);
        }
        foreach (var field in typeDef.Fields) {
          if (field.ExecutionType != FieldExecutionType.NotSet)
            continue;
          if (field.ClrMember == null)
            continue; //__typename has no clr member
          var methName = field.ClrMember.Name;
          var method = allMethods.FirstOrDefault(m => m.Name == methName);
          if (method == null)
            continue;
          SetupFieldResolverMethod(typeDef, field, method, null);
        } //foreach field
      } //foreach typeDef
    }//method

    private bool SetupFieldResolverMethod(ObjectTypeDef typeDef, FieldDef field, MethodInfo resolverMethod, Attribute sourceAttr) {
      var retType = resolverMethod.ReturnType;
      var returnsTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
      Func<object, object> taskResultReader = null;
      if (returnsTask) {
        retType = retType.GetGenericArguments()[0];
        taskResultReader = ServerReflectionHelper.CompileTaskResultReader(retType);
      }
      // validate return type
      if (!CheckReturnTypeCompatible(retType, field, resolverMethod))
        return false; 

      var resMethInfo =  new ResolverMethodInfo() { SourceAttribute = sourceAttr, Method = resolverMethod, 
            ResolverClass = resolverMethod.DeclaringType,
          ReturnsTask = returnsTask, TaskResultReader = taskResultReader };
      var fldResolver = new FieldMapping() { Field = field, ResolverInfo = resMethInfo };
      field.Resolver = resMethInfo; 
      if (returnsTask)
        field.Flags |= FieldFlags.ResolverReturnsTask;
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
