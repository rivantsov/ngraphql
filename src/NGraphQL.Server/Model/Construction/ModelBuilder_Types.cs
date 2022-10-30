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
using System.ComponentModel;

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

    private void BuildTypesInternalsFromClrType() {
      foreach (var td in _model.Types) {
        if (td.ClrType == null)
          continue; // Special types (Query, Mutation etc) do not have Clr types
        DirectiveLocation loc = DirectiveLocation.None;
        switch (td) {

          case InterfaceTypeDef intfTypeDef:
            loc = DirectiveLocation.Interface;
            BuildComplexTypeFields(intfTypeDef);
            break;

          case InputObjectTypeDef inpTypeDef:
            loc = DirectiveLocation.InputObject;
            BuildComplexTypeFields(inpTypeDef);
            break;

          case ObjectTypeDef objTypeDef:
            loc = DirectiveLocation.Object;
            BuildComplexTypeFields(objTypeDef);
            break;

          case EnumTypeDef etd:
            loc = DirectiveLocation.Enum;
            BuildEnumTypeFields(etd);
            break;

          case UnionTypeDef utd:
            loc = DirectiveLocation.Union;
            // we build union types in a separate loop after building other types
            break;
        } //switch
        td.Directives = BuildDirectivesFromAttributes(td.ClrType, loc);
      } //foreach td
    }

    // used for interface and Object types
    private void BuildComplexTypeFields(ComplexTypeDef typeDef) {
      var objTypeDef = typeDef as ObjectTypeDef;
      var clrType = typeDef.ClrType;
      var members = clrType.GetFieldsPropsMethods(withMethods: true);
      foreach (var member in members) {
        var attrs = GetAllAttributesAndAdjustments(member);
        var ignoreAttr = attrs.Find<IgnoreAttribute>();
        if (ignoreAttr != null)
          continue;
        var retType = member.GetMemberReturnType(); //unwraps Task<t>
        var typeRef = GetMemberGraphQLTypeRef(retType, member, $"Field {clrType.Name}.{member.Name}");
        if (typeRef == null)
          continue; //error should be logged already
        var name = GetGraphQLName(member);
        var descr = _docLoader.GetDocString(member, clrType);
        var fld = new FieldDef(typeDef, name, typeRef) { ClrMember = member, Description = descr, Attributes = attrs };
        fld.Directives = BuildDirectivesFromAttributes(member, DirectiveLocation.FieldDefinition);
        if (attrs.Find<HiddenAttribute>() != null)
          fld.Flags |= FieldFlags.Hidden;
        typeDef.Fields.Add(fld);
        if (member is MethodInfo method)
          BuildFieldArguments(fld, method);
        if (typeDef.Kind == TypeKind.InputObject)
          ProcessInputObjectField(fld);
      }
    }

    private void ProcessInputObjectField(FieldDef field) {
      var typeRef = field.TypeRef;
      if (typeRef == null)
        return; // error found, it is already logged
      var mtype = typeRef.TypeDef.ClrType;
      if (field.Args.Count > 0) {
        AddError($"Input type member {field.FullRefName}: input fields may not have arguments.");
        return;
      }
      if (typeRef.IsList && !typeRef.TypeDef.IsEnumFlagArray()) {
        // list members must be IList<T> or T[] - this is important, lists are instantiated as arrays when deserializing
        if (!mtype.IsArray && !mtype.IsInterface) {
          AddError($"Input type member {field.FullRefName}: list must be either array or IList<T>.");
          return;
        }
      }
      switch (typeRef.TypeDef.Kind) {
        case TypeKind.Scalar:
        case TypeKind.Enum:
        case TypeKind.InputObject:
          break;
        default:
          AddError($"Input type member {field.FullRefName}: invalid type {mtype}; must be scalar, enum or input type.");
        break; 
      }
      // check default value from DefaultValueAttribute
      var dftValAttr = field.ClrMember.GetAttribute<DefaultValueAttribute>();
      if (dftValAttr != null) {
        field.HasDefaultValue = true;
        field.DefaultValue = dftValAttr.Value;
      }
    }

    private void BuildEnumTypeFields(EnumTypeDef enumTypeDef) {
      enumTypeDef.Handler = new EnumHandler(enumTypeDef.ClrType, enumTypeDef.Module.Adjustments);
      enumTypeDef.Name = enumTypeDef.Handler.EnumName;
      // Build fieldDefs from EnumInfo.Values
      foreach (var vi in enumTypeDef.Handler.Values) {
        var enumFld = new EnumFieldDef() { Name = vi.Name, ValueInfo = vi };
        enumFld.Description = _docLoader.GetDocString(enumFld.ValueInfo.Field, enumTypeDef.ClrType);
        enumFld.Directives = this.BuildDirectivesFromAttributes(enumFld.ValueInfo.Field, DirectiveLocation.EnumValue);
        enumTypeDef.Fields.Add(enumFld);
      }
    }

    private void BuildUnionTypes() {
      var unionTypes = _model.Types.Where(td => td.Kind == TypeKind.Union).ToList();
      foreach (UnionTypeDef utDef in unionTypes) {
        var objTypes = utDef.ClrType.BaseType.GetGenericArguments();
        foreach (var objType in objTypes) {
          var typeDef = _model.GetTypeDef(objType);
          if (typeDef == null) {
            AddError($"Union type {utDef.Name}: type {objType} is not registered.");
            continue;
          }
          if (typeDef is ObjectTypeDef objTypeDef)
            utDef.PossibleTypes.Add(objTypeDef);
          else
            AddError($"Union type {utDef.Name}: type {objType} is not GraphQL 'type'.");
        }
      } //foreach unionType
    }


    private void BuildFieldArguments(FieldDef fieldDef, MethodInfo resMethod) {
      var prms = resMethod.GetParameters();
      if (prms == null || prms.Length == 0)
        return;
      fieldDef.Args = BuildArgDefs(prms, resMethod);
    }

    private IList<InputValueDef> BuildArgDefs(IList<ParameterInfo> parameters, MethodBase method) {
      var argDefs = new List<InputValueDef>();
      foreach (var prm in parameters) {
        var attrs = GetAllAttributesAndAdjustments(prm, method);
        var prmTypeRef = GetMemberGraphQLTypeRef(prm.ParameterType, prm, $"Method {method.Name}, parameter {prm.Name}", method);
        if (prmTypeRef == null)
          continue;
        if (prmTypeRef.IsList && !prmTypeRef.TypeDef.IsEnumFlagArray())
          VerifyListParameterType(prm.ParameterType, method, prm.Name);
        var dftValue = prm.DefaultValue == DBNull.Value ? null : prm.DefaultValue;
        // special case: if default value is null, it is nullable
        if (prm.HasDefaultValue && dftValue == null && prmTypeRef.Kind == TypeKind.NonNull)
          prmTypeRef = prmTypeRef.Inner; // nullable
        var argDef = new InputValueDef() {
          Name = GetGraphQLName(prm), TypeRef = prmTypeRef, Attributes = attrs,
          ParamType = prm.ParameterType, HasDefaultValue = prm.HasDefaultValue, DefaultValue = dftValue
        };
        argDef.Directives = BuildDirectivesFromAttributes(prm, DirectiveLocation.ArgumentDefinition);
        argDefs.Add(argDef);
      }
      return argDefs;
    }

    private TypeRef GetMemberGraphQLTypeRef(Type type, ICustomAttributeProvider attributeSource, string location, MethodBase paramOwner = null) {
      var scalarAttr = attributeSource.GetAttribute<ScalarAttribute>();

      UnwrapClrType(type, attributeSource, out var baseType, out var kinds, paramOwner);

      TypeDefBase typeDef;
      if (scalarAttr != null) {
        typeDef = _model.LookupTypeDef(scalarAttr.ScalarName);
        if (type == null) {
          AddError($"{location}: type {scalarAttr.ScalarName} is not defined. ");
          return null;
        }
      } else if (null == (typeDef = _model.GetTypeDef(baseType))) {
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
