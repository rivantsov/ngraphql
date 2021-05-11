using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;
using NGraphQL.Model;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    private bool RegisterScalars() {
      foreach (var module in _server.Modules) {
        var mName = module.Name;
        // scalars
        foreach (var scalarType in module.ScalarTypes) {
          var scalar = (Scalar)Activator.CreateInstance(scalarType);
          var sTypeDef = new ScalarTypeDef(scalar, module);
          RegisterTypeDef(sTypeDef);
        }
      }
      return !_model.HasErrors;
    }

    private void RegisterResolverClassesMethods() {
      var flags = BindingFlags.Public | BindingFlags.Instance;
      foreach (var module in _server.Modules) {
        foreach (var resClass in module.ResolverClasses) {
          var resClassInfo = new ResolverClassInfo() { Module = module, Type = resClass };
          _model.ResolverClasses.Add(resClassInfo);
          var methods = resClass.GetMethods(flags);
          foreach(var m in methods) {
            var resAttr = m.GetAttribute<ResolvesFieldAttribute>();
            var resInfo = new ResolverMethodInfo() {
              Method = m, Module = module, ResolverClass = resClassInfo, ReturnsTask = m.MethodReturnsTask(),
              ReturnType = m.GetReturnDataType(), ResolvesAttribute = resAttr
            };
            if (resInfo.ReturnsTask)
              resInfo.TaskResultReader = ServerReflectionHelper.CompileTaskResultReader(m.ReturnType);
            _allResolvers.Add(resInfo);
          }
        }
      }
    }

    private bool RegisterGraphQLTypes() {
      foreach (var module in _server.Modules) {
        var mName = module.Name;
        // other types
        foreach (var type in module.EnumTypes)
          CreateRegisterTypeDef(type, module, TypeKind.Enum);
        foreach (var type in module.ObjectTypes)
          CreateRegisterTypeDef(type, module, TypeKind.Object);
        foreach (var type in module.InputTypes)
          CreateRegisterTypeDef(type, module, TypeKind.InputObject);
        foreach (var type in module.InterfaceTypes)
          CreateRegisterTypeDef(type, module, TypeKind.Interface);
        foreach (var type in module.UnionTypes)
          CreateRegisterTypeDef(type, module, TypeKind.Union);
        // Query, Mutation, Subscription
        RegisterSpecialObjectTypeIfProvided(module.QueryType, TypeRole.ModuleQuery, module);
        RegisterSpecialObjectTypeIfProvided(module.MutationType, TypeRole.ModuleMutation, module);
        RegisterSpecialObjectTypeIfProvided(module.SubscriptionType, TypeRole.ModuleSubscription, module);
      } // foreach module

      return !_model.HasErrors;
    } //method

    private void RegisterSpecialObjectTypeIfProvided(Type type, TypeRole typeRole, GraphQLModule module) {
      if (type == null)
        return;
      var typeName = $"{module.Name}_{type.Name}";
      var typeDef = new ObjectTypeDef(typeName, type, GraphQLModelObject.EmptyAttributeList, module, typeRole);
      _model.Types.Add(typeDef);
      _model.TypesByClrType.Add(type, typeDef); 
    }

    private void CreateRegisterTypeDef(Type type, GraphQLModule module, TypeKind typeKind) {
      try {
        var typeDef = CreateTypeDef(type, typeKind, module);
        if (typeDef == null)
          return;
        var hideAttr = typeDef.Attributes.Find<HiddenAttribute>();
        if (hideAttr != null)
          typeDef.Hidden = true;
        if (typeDef.ClrType != null && typeDef.Kind != TypeKind.Scalar) { //schema has no CLR type
          typeDef.Description = _docLoader.GetDocString(typeDef.ClrType, typeDef.ClrType);
        }
        RegisterTypeDef(typeDef); 
      } catch (Exception ex) {
        AddError($"FATAL: Failed to register type {type}, error: {ex}. ");
      }
    }

    private void RegisterTypeDef(TypeDefBase typeDef) {
      _model.Types.Add(typeDef);
      // data types - we register them by name and CLR type; they always have module and CLR type
      var modName = typeDef.Module?.Name ?? "(no module)";
      if (typeDef.ClrType != null && typeDef.IsDefaultForClrType) {
        if (_model.TypesByClrType.ContainsKey(typeDef.ClrType)) {
          AddError($"Duplicate registration of type {typeDef.Name} as default for CLR type {typeDef.ClrType}, module {modName}.");
          return;
        }
        _model.TypesByClrType.Add(typeDef.ClrType, typeDef);
      }
      if (_model.TypesByName.ContainsKey(typeDef.Name)) {
        AddError($"GraphQL type {typeDef.Name} already registered; module: {modName}.");
        return;
      }
      _model.TypesByName.Add(typeDef.Name, typeDef);
    }

  }//class
}
