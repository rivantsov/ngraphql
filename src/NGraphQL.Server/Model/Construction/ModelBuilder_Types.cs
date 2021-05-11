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

    private TypeDefBase CreateTypeDef (Type type, TypeKind typeKind, GraphQLModule module) {
      var allAttrs = GetAllAttributesAndAdjustments(type);
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
      var attrs = GetAllAttributesAndAdjustments(attributeSource, paramOwner);
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
