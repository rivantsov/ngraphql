using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Introspection;
using NGraphQL.Server;
using NGraphQL.Utilities;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Core;
using System.Runtime.InteropServices.ComTypes;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {
    GraphQLApi _api;
    GraphQLApiModel _model;
    XmlDocumentationLoader _docLoader;

    public ModelBuilder(GraphQLApi api) {
      _api = api;
    }

    public void Build() {
      _model = _api.Model;

      if (_model.HasErrors)
        return;

      // connect interfaces
      RegisterImplementedInterfaces();

      // collect mappings
      AssignMappedEntities();
      if (_model.HasErrors)
        return;

      BuildUnionTypes();
      if (_model.HasErrors)
        return;

      // add self-mapped types (add their mappings to themselves)
      AssignSelfMappedObjectTypes();

      BuildTypesInternals();
      if (_model.HasErrors)
        return;

      ProcessEntityMappings();
      if (_model.HasErrors)
        return;

      ProcessResolverClasses();
      if (_model.HasErrors)
        return;


      BuildSchemaDef();
      if (_model.HasErrors)
        return;

      var introSchemaBuilder = new IntrospectionSchemaBuilder();
      introSchemaBuilder.Build(_model);
      if (_model.HasErrors)
        return;

      var schemaGen = new SchemaDocGenerator();
      _model.SchemaDoc = schemaGen.GenerateSchema(_model);

      foreach (var module in _api.Modules)
        module.OnModelConstructed();

      VerifyModel(); 
    }

    public void AddError(string message) {
      _model.Errors.Add(message); 
    }

    private void RegisterImplementedInterfaces() {
      var objTypes = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object);
      foreach(var typeDef in objTypes) {
        var intTypes = typeDef.ClrType.GetInterfaces();
        foreach(var iType in intTypes) {
          var iTypeDef = (InterfaceTypeDef) _model.LookupTypeDef(iType);
          if (iTypeDef != null) {
            typeDef.Implements.Add(iTypeDef);
            iTypeDef.PossibleTypes.Add(typeDef); 
          }
        }
      } //foreach typeDef
    }

    private void BuildInputObjectFields(InputObjectTypeDef inpTypeDef) {
      var members = inpTypeDef.ClrType.GetFieldsProps();
      foreach(var member in members) {
        var mtype = member.GetMemberType();
        var typeRef = GetTypeRef(mtype, member, $"Field {inpTypeDef.Name}.{member.Name}");
        if (typeRef.IsList && !typeRef.TypeDef.IsEnumFlagArray()) {
          // list members must be IList<T> or T[] - this is important, lists are instantiated as arrays 
          // when deserializing
          if (!mtype.IsArray && !mtype.IsInterface) {
            AddError($"Input type member {inpTypeDef.Name}.{member.Name}: list must be either array or IList<T>.");
            continue;
          }
        }
        switch(typeRef.TypeDef.Kind) {
          case TypeKind.Scalar:
          case TypeKind.Enum:
          case TypeKind.InputObject:
            break;
          default:
            AddError($"Input type member {inpTypeDef.Name}.{member.Name}: type {mtype} is not scalar or input type.");
            continue; 
        }

        var dirs = BuildDirectivesFromAttributes(member);
        var inpFldDef = new InputValueDef() { Name = member.Name.FirstLower(), TypeRef = typeRef, InputObjectClrMember = member,
          Directives = dirs, Description = _docLoader.GetDocString(member, member.DeclaringType)
        };
        inpTypeDef.Fields.Add(inpFldDef);
      } //foreach
    }

    private void BuildUnionTypes() {
      var unionTypes = _model.Types.Where(td => td.Kind == TypeKind.Union).ToList();
      foreach(UnionTypeDef utDef in unionTypes) {
        var objTypes = utDef.ClrType.BaseType.GetGenericArguments();
        foreach(var objType in objTypes) {
          var typeDef = _model.LookupMappedTypeDef(objType);
          if(typeDef == null) {
            AddError($"Union type {utDef.Name}: type {objType} is not registered.");
            continue;
          }
          if(typeDef is ObjectTypeDef objTypeDef)
            utDef.PossibleTypes.Add(objTypeDef);
          else
            AddError($"Union type {utDef.Name}: type {objType} is not GraphQL 'type'.");
        }
      } //foreach unionType
    }

    private void BuildSchemaDef() {
      var schemaDef =_model.Schema = new ObjectTypeDef("Schema", null);
      RegisterTypeDef(schemaDef);
      schemaDef.Fields.Add(new FieldDef("query", _model.QueryType.TypeRefNull));
      if(_model.MutationType != null)
        schemaDef.Fields.Add(new FieldDef("mutation", _model.MutationType.TypeRefNull));
      if(_model.SubscriptionType != null)
        schemaDef.Fields.Add(new FieldDef("subscription", _model.SubscriptionType.TypeRefNull));
      // add the schema itself as '__schema' to query
      var schField = new FieldDef("__schema", schemaDef.TypeRefNull);
      schField.Flags |= FieldFlags.Hidden; 
      _model.QueryType.Fields.Add(schField);
      // mark special types
      _model.Schema.IsSpecialType = true;
      _model.QueryType.IsSpecialType = true;
      if (_model.MutationType != null)
        _model.MutationType.IsSpecialType = true;
      if (_model.SubscriptionType != null)
        _model.SubscriptionType.IsSpecialType = true;
    }

    private void BuildEnumValues(EnumTypeDef enumTypeDef) {
      // enum values are static public fields of enum type
      var fields = enumTypeDef.ClrType.GetFields(BindingFlags.Public | BindingFlags.Static); 
      foreach(var fld in fields) {
        var ignoreAttr = fld.GetCustomAttribute<IgnoreAttribute>();
        if(ignoreAttr != null)
          continue; 
        var fldValue = fld.GetValue(null);
        var longValue = Convert.ToInt64(fldValue);
        if(longValue == 0 && enumTypeDef.IsFlagSet)
          continue; // ignore 'None=0' member in Flags enum
        var dirs = BuildDirectivesFromAttributes(fld); 
        var enumV = new EnumValue() {
          Name = GetEnumFieldGraphQLName(fld), ClrValue = fldValue, ClrName = fldValue.ToString(), LongValue = longValue, 
          Description = _docLoader.GetDocString(fld, enumTypeDef.ClrType)
        };
        enumTypeDef.EnumValues.Add(enumV);
      }
    }

    private TypeRef GetTypeRef(Type type, ICustomAttributeProvider attributeSource, string location) {
      var scalarAttr = attributeSource.GetAttribute<ScalarAttribute>(); 

      UnwrapClrType(type, attributeSource, out var baseType, out var kinds);

      TypeDefBase typeDef;
      if (scalarAttr != null) {
        typeDef = _model.GetScalarTypeDef(scalarAttr.ScalarName);
        if (type == null) {
          AddError($"{location}: scalar type {scalarAttr.ScalarName} is not defined. ");
          return null;
        }
      } else if(_model.EntityMappings.TryGetValue(baseType, out var entMapping))
        typeDef = entMapping.TypeDef;
      else if(!_model.TypesByClrType.TryGetValue(baseType, out typeDef) ) {
        AddError($"{location}: type {baseType} is not registered. ");
        return null;
      }

      // add typeDef kind to kinds list and find/create type ref
      var allKinds = new List<TypeKind>();
      allKinds.Add(typeDef.Kind);

      // Flags enums are represented by enum arrays
      if(typeDef.IsEnumFlagArray()) {
        allKinds.Add(TypeKind.NotNull);
        allKinds.Add(TypeKind.List);
      }
      
      allKinds.AddRange(kinds);
      var typeRef = typeDef.GetCreateTypeRef(allKinds);
      return typeRef; 
    }
    
    private IList<Directive> BuildDirectivesFromAttributes(ICustomAttributeProvider target) {
      var attrList = target.GetCustomAttributes(inherit: true);
      if(attrList.Length == 0)
        return Directive.EmptyList;

      var dirList = new List<Directive>();
      foreach(var attr in attrList) {
        if(!(attr is DirectiveBaseAttribute dirAttr))
          continue;
        var attrName = attr.GetType().Name;
        var dirDefType = dirAttr.DirectiveDefType;
        var dirDef = _model.Directives.Values.FirstOrDefault(def => def.GetType() == dirDefType);
        if(dirDef == null) {
          AddError($"{target}: directive definition {dirDefType.Name} referenced by [{attrName}] not registered..");
          continue;
        }
        var attrDirDef = dirDef as AttributeBasedDirectiveDef;
        if(attrDirDef == null) {
          AddError($"{target}: directive {dirDefType.Name} cannot be created from attribute.");
          continue;
        }
        var dir = attrDirDef.CreateDirective(_model, dirAttr, target);
        dirList.Add(dir);
      }
      return dirList;
    } //method

    private void UnwrapClrType(Type type, ICustomAttributeProvider attributeSource, out Type baseType, out List<TypeKind> kinds) {
      kinds = new List<TypeKind>();
      bool notNull = attributeSource.GetAttribute<NullAttribute>() == null;
      Type valueTypeUnder;

      if(type.IsGenericListOrArray(out baseType, out var rank)) {
        valueTypeUnder = Nullable.GetUnderlyingType(baseType);
        baseType = valueTypeUnder ?? baseType;
        var withNulls = attributeSource.GetAttribute<WithNullsAttribute>() != null || valueTypeUnder != null;
        if(!withNulls)
          kinds.Add(TypeKind.NotNull);
        for(int i = 0; i < rank; i++)
          kinds.Add(TypeKind.List);
        if(notNull)
          kinds.Add(TypeKind.NotNull);
        return;
      }

      // not array      
      baseType = type;
      // check for nullable value type
      valueTypeUnder = Nullable.GetUnderlyingType(type);
      if(valueTypeUnder != null) {
        baseType = valueTypeUnder;
        notNull = false;
      }

      if(notNull)
        kinds.Add(TypeKind.NotNull);
    }


    private void VerifyModel() {
      foreach(var typeDef in _model.Types) {
        switch(typeDef) {
          case ObjectTypeDef otd:
            if (!otd.IsSpecialType)
              VerifyObjectType(otd); 
            break;
          case InputObjectTypeDef itd:
            break; 
        }
      }
    }

    private void VerifyObjectType(ObjectTypeDef typeDef) {
      foreach(var field in typeDef.Fields) {
        // so far we have only exec type to set, or post error
        if (field.Reader != null)
          field.ExecutionType = FieldExecutionType.Reader;
        else if (field.Resolver != null)
          field.ExecutionType = FieldExecutionType.Resolver;
        else 
          AddError($"Field {typeDef.Name}.{field.Name} has no associated resolver or mapped entity field.");
      }
    }

  } //class
}
