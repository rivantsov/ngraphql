using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {
    GraphQLServer _server;
    GraphQLApiModel _model;
    XmlDocumentationLoader _docLoader;
    IList<ModelAdjustment> _modelAdjustments;
    IList<ResolverMethodInfo> _allResolvers = new List<ResolverMethodInfo>();

    public ModelBuilder(GraphQLServer server) {
      _server = server;
      _docLoader = new XmlDocumentationLoader();
    }

    public void BuildModel() {
      _model = _server.Model = new GraphQLApiModel();
      _modelAdjustments = _server.Modules.SelectMany(m => m.Adjustments).ToList();

      // collect,register scalar, data types, query/mutation types, resolvers
      RegisterScalars();
      if (!RegisterGraphQLTypes())
        return;

      RegisterResolverClassesMethods();

      if (!BuildRegisteredDirectiveDefinitions())
        return;


      BuildTypesInternalsFromClrType();
      if (_model.HasErrors)
        return;

      if (!InitializeTypeMappings())
        return;


      LinkImplementedInterfaces();
      BuildUnionTypes();
      if (_model.HasErrors)
        return;

      AssignObjectFieldResolvers();
      if (_model.HasErrors)
        return;

      BuildSchemaDef();
      if (_model.HasErrors)
        return;

      var introSchemaBuilder = new IntrospectionSchemaBuilder();
      introSchemaBuilder.Build(_model);
      if (_model.HasErrors)
        return;

      // apply directives to all model objects in the model
      _model.ApplyToAllModelObjects(this.ApplyDirectives);
      if (_model.HasErrors)
        return;

      var schemaGen = new SchemaDocGenerator();
      _model.SchemaDoc = schemaGen.GenerateSchema(_model);

      VerifyModel();
    }

    private bool InitializeTypeMappings() {
      foreach (var module in _server.Modules) {
        var mname = module.GetType().Name;
        foreach (var entMapping in module.EntityMappings) {
          var typeDef = _model.GetTypeDef(entMapping.GraphQLType);
          if (typeDef == null) {
            AddError($"Mapping target type {entMapping.GraphQLType.Name} is not registered; module {mname}");
            continue;
          }

          if (!(typeDef is ObjectTypeDef objTypeDef)) {
            AddError($"Invalid mapping target type {entMapping.GraphQLType.Name}, must be Object type; module {mname}");
            continue;
          }
          var typeMapping = new ObjectTypeMapping(objTypeDef, entMapping.EntityType, entMapping.Expression);
          RegisterTypeMapping(typeMapping);
        } // foreach mapping
      }
      // Add self-maps to all objects, including module-level Query, Mutation types
      foreach (var typeDef in _model.Types) {
        if (typeDef is ObjectTypeDef otd) {
          var mappingExt = new ObjectTypeMapping(otd, otd.ClrType);
          otd.Mappings.Add(mappingExt);
        }
      }
      return !_model.HasErrors;
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

          case ObjectTypeDef objTypeDef:
            loc = DirectiveLocation.Object;
            BuildComplexTypeFields(objTypeDef);
            break;

          case InputObjectTypeDef inpTypeDef:
            loc = DirectiveLocation.InputObject;
            BuildInputObjectFields(inpTypeDef);
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

    private void LinkImplementedInterfaces() {
      var objTypes = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object);
      foreach (var typeDef in objTypes) {
        if (typeDef.ClrType == null) //exclude Intro objects and special types
          continue;
        var intTypes = typeDef.ClrType.GetInterfaces();
        foreach (var iType in intTypes) {
          var iTypeDef = (InterfaceTypeDef)_model.GetTypeDef(iType);
          if (iTypeDef != null) {
            typeDef.Implements.Add(iTypeDef);
            iTypeDef.PossibleTypes.Add(typeDef);
          }
        }
      } //foreach typeDef
    }

    private void BuildInputObjectFields(InputObjectTypeDef inpTypeDef) {
      var members = inpTypeDef.ClrType.GetFieldsProps();
      foreach (var member in members) {
        var attrs = GetAllAttributesAndAdjustments(member);
        var mtype = member.GetMemberReturnType();
        var typeRef = GetMemberGraphQLTypeRef(mtype, member, $"Field {inpTypeDef.Name}.{member.Name}");
        if (typeRef == null)
          return; // error found, it is already logged
        if (typeRef.IsList && !typeRef.TypeDef.IsEnumFlagArray()) {
          // list members must be IList<T> or T[] - this is important, lists are instantiated as arrays when deserializing
          if (!mtype.IsArray && !mtype.IsInterface) {
            AddError($"Input type member {inpTypeDef.Name}.{member.Name}: list must be either array or IList<T>.");
            continue;
          }
        }
        switch (typeRef.TypeDef.Kind) {
          case TypeKind.Scalar:
          case TypeKind.Enum:
          case TypeKind.InputObject:
            break;
          default:
            AddError($"Input type member {inpTypeDef.Name}.{member.Name}: type {mtype} is not scalar or input type.");
            continue;
        }
        var inpFldDef = new InputValueDef() { Name = member.Name.FirstLower(), TypeRef = typeRef, Attributes = attrs,
          InputObjectClrMember = member,
          Description = _docLoader.GetDocString(member, member.DeclaringType)
        };
        inpFldDef.Directives = BuildDirectivesFromAttributes(member, DirectiveLocation.InputFieldDefinition);
        inpTypeDef.Fields.Add(inpFldDef);
      } //foreach
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
      foreach(UnionTypeDef utDef in unionTypes) {
        var objTypes = utDef.ClrType.BaseType.GetGenericArguments();
        foreach(var objType in objTypes) {
          var typeDef = _model.GetTypeDef(objType);
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
      _model.QueryType = BuildRootSchemaObject("Query", TypeRole.Query, TypeRole.ModuleQuery);
      _model.MutationType = BuildRootSchemaObject("Mutation", TypeRole.Mutation, TypeRole.ModuleMutation);
      _model.SubscriptionType = BuildRootSchemaObject("Subscription", TypeRole.Subscription, TypeRole.ModuleSubscription);

      if (_model.QueryType == null) {
        AddError("No fields are registered for Query root type; must have at least one query field.");
        return; 
      }
      var noAttrs = GraphQLModelObject.EmptyAttributeList;
      var schemaDef = _model.Schema = new ObjectTypeDef("Schema", null, noAttrs, null, TypeRole.Schema);
      RegisterTypeDef(schemaDef);
      schemaDef.Hidden = false; 
      schemaDef.Fields.Add(new FieldDef(schemaDef, "query", _model.QueryType.TypeRefNull));
      if (_model.MutationType != null)
        schemaDef.Fields.Add(new FieldDef(schemaDef, "mutation", _model.MutationType.TypeRefNull));
      if (_model.SubscriptionType != null)
        schemaDef.Fields.Add(new FieldDef(schemaDef, "subscription", _model.SubscriptionType.TypeRefNull));
    }

    private ObjectTypeDef BuildRootSchemaObject(string name, TypeRole typeRole, TypeRole moduleTypeRole) {
      var allModuleAggrTypes = _model.Types.OfType<ObjectTypeDef>()
                        .Where(t => t.TypeRole == moduleTypeRole)
                        .ToList();
      if (allModuleAggrTypes.Count == 0)
        return null;
      // create root object (ex: Query type)
      var rootObjTypeDef = new ObjectTypeDef(name, null, GraphQLModelObject.EmptyAttributeList, null, typeRole);
      RegisterTypeDef(rootObjTypeDef);
      var mapping = new ObjectTypeMapping(rootObjTypeDef, null);
      rootObjTypeDef.Mappings.Add(mapping); 
      // copy resolvers
      foreach (var aggrType in allModuleAggrTypes) 
        mapping.FieldResolvers.AddRange(aggrType.Mappings[0].FieldResolvers);
      // collect all fields from resolvers
      var allFields = mapping.FieldResolvers.Select(fr => fr.Field).ToList();
      rootObjTypeDef.Fields.AddRange(allFields);
      // check for name duplicates
      var fieldNameDupes = rootObjTypeDef.Fields.Select(f => f.Name).GroupBy(fn => fn).Where(g => g.Count() > 1).ToList();
      if (fieldNameDupes.Count > 0) {
        string dupesAll = string.Join(",", fieldNameDupes.Select(g => g.Key));
        AddError($"Duplicate fields defined at top-level type {typeRole}, field names: {dupesAll}");
      }
      return rootObjTypeDef;
    }

    private void VerifyModel() {
    }



  } //class
}
