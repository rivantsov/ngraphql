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

    private void RegisterResolverClasses() {
      foreach (var module in _server.Modules) {
        foreach (var resType in module.ResolverTypes)
          _model.Resolvers.Add(new ResolverClassInfo() { Module = module, Type = resType });
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

    private TypeDefBase CreateTypeDef (Type type, TypeKind typeKind, GraphQLModule module) {
      var allAttrs = GetAllAttributes(type);
      var nameAttr = allAttrs.Find<GraphQLNameAttribute>();
      var typeName = nameAttr?.Name ?? GetGraphQLName(type);
      var moduleName = module.Name;

      switch (typeKind) {
        case TypeKind.Enum:
          if (!type.IsEnum) {
            AddError($"Type {type} cannot be registered as Enum GraphQL type, must be enum; module: {moduleName}");
            return null; 
          }
          return new EnumTypeDef(type, allAttrs, module);

        case TypeKind.Object:
          return new ObjectTypeDef(typeName, type, allAttrs, module);
        case TypeKind.Interface:
          return new InterfaceTypeDef(typeName, type, allAttrs, module);
        case TypeKind.InputObject:
          return new InputObjectTypeDef(typeName, type, allAttrs, module);
        case TypeKind.Union:
          return new UnionTypeDef(typeName, type, allAttrs, module);
      }
      // should never happen
      return null;
    }

    private TypeRef GetTypeRef(Type type, ICustomAttributeProvider attributeSource, string location, MethodBase paramOwner = null) {
      var scalarAttr = attributeSource.GetAttribute<ScalarAttribute>();

      UnwrapClrType(type, attributeSource, out var baseType, out var kinds, paramOwner);

      TypeDefBase typeDef;
      if (scalarAttr != null) {
        typeDef = _model.GetScalarTypeDef(scalarAttr.ScalarName);
        if (type == null) {
          AddError($"{location}: scalar type {scalarAttr.ScalarName} is not defined. ");
          return null;
        }
      } else if (_model.TypesByEntityType.TryGetValue(baseType, out var mappedTypeDef))
        typeDef = mappedTypeDef;
      else if (!_model.TypesByClrType.TryGetValue(baseType, out typeDef)) {
        AddError($"{location}: type {baseType} is not registered. ");
        return null;
      }

      // add typeDef kind to kinds list and find/create type ref
      var allKinds = new List<TypeKind>();
      allKinds.Add(typeDef.Kind);

      // Flags enums are represented by enum arrays
      if (typeDef.IsEnumFlagArray()) {
        allKinds.Add(TypeKind.NonNull);
        allKinds.Add(TypeKind.List);
      }

      allKinds.AddRange(kinds);
      var typeRef = typeDef.GetCreateTypeRef(allKinds);
      return typeRef;
    }

    private void UnwrapClrType(Type type, ICustomAttributeProvider attributeSource, out Type baseType, out List<TypeKind> kinds, 
                                  MethodBase paramOwner) {
      kinds = new List<TypeKind>();
      var attrs = GetAllAttributes(attributeSource, paramOwner);
      bool notNull = attrs.Find<NullAttribute>() == null;
      Type valueTypeUnder;

      if (type.IsGenericListOrArray(out baseType, out var rank)) {
        valueTypeUnder = Nullable.GetUnderlyingType(baseType);
        baseType = valueTypeUnder ?? baseType;
        var withNulls = attrs.Find<WithNullsAttribute>() != null || valueTypeUnder != null;
        if (!withNulls)
          kinds.Add(TypeKind.NonNull);
        for (int i = 0; i < rank; i++)
          kinds.Add(TypeKind.List);
        if (notNull)
          kinds.Add(TypeKind.NonNull);
        return;
      }

      // not array      
      baseType = type;
      // check for nullable value type
      valueTypeUnder = Nullable.GetUnderlyingType(type);
      if (valueTypeUnder != null) {
        baseType = valueTypeUnder;
        notNull = false;
      }

      if (notNull)
        kinds.Add(TypeKind.NonNull);
    }

  }//class
}
