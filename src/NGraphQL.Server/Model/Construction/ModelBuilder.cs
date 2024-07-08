using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

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
    IList<ResolverMethodInfo> _allResolverMethods = new List<ResolverMethodInfo>();

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


      LinkValidateInterfaces();
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

      // apply model directives to all model objects in the model

      ApplyModelDirectives();
      if (_model.HasErrors)
        return;

      CompleteInitScalars();
      if (_model.HasErrors)
        return;

      MarkIntrospectionTypeDefs();

      var schemaDocGen = new SchemaDocGenerator();
      _model.SchemaDoc = schemaDocGen.GenerateSchema(_model);
    }

    private void MarkIntrospectionTypeDefs() {
      foreach(var type in _server.IntrospectionModule.EnumTypes)
        _model.GetTypeDef(type).IsIntrospectionType = true;
      foreach (var type in _server.IntrospectionModule.ObjectTypes)
        _model.GetTypeDef(type).IsIntrospectionType = true;
    }

    private void ApplyModelDirectives() {
      var visitor = new ModelVisitor(_model);
      visitor.Visit(ApplyDirectives);
    }
    
    private void ApplyDirectives(GraphQLModelObject modelObj) {
      if (!modelObj.HasDirectives())
        return;
      foreach (var dir in modelObj.Directives) {
          dir.Def.Handler.ModelDirectiveApply(_model, modelObj, dir.ArgValues);
      }
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
      // collect all fields from collected resolvers
      var allFields = mapping.FieldResolvers.Select(fr => fr.Field).ToList();
      rootObjTypeDef.Fields.AddRange(allFields);
      // check for name duplicates
      var fieldNameDupes = rootObjTypeDef.Fields.Select(f => f.Name).GroupBy(fn => fn).Where(g => g.Count() > 1).ToList();
      if (fieldNameDupes.Count > 0) {
        string dupesAll = string.Join(",", fieldNameDupes.Select(g => g.Key));
        AddError($"Duplicate fields defined at top-level type {typeRole}, field names: {dupesAll}");
      }
      // important: re-assign Index value for all fields; we moved fields to aggregate Query, Mutation
      //  objects, so their indexes changed
      ReassignFieldIndexes(rootObjTypeDef); 
      return rootObjTypeDef;
    }

    // complete initialization of scalars, provide them with copy of the model;
    // Ex: AnyScalar uses Model to get refs to other scalars.
    private void CompleteInitScalars() {
      foreach (var tdef in _model.Types)
        if (tdef is ScalarTypeDef stdef)
          stdef.Scalar.CompleteInit(_model); 
    }

  } //class
}
