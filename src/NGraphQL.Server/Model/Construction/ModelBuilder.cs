using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NGraphQL.CodeFirst;
using NGraphQL.CodeFirst.Internals;
using NGraphQL.Core;
using NGraphQL.Core.Scalars;
using NGraphQL.Internals;
using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {
    GraphQLServer _server; 
    GraphQLApiModel _model;
    XmlDocumentationLoader _docLoader;
    IList<ModelAdjustment> _modelAdjustments; 

    public ModelBuilder(GraphQLServer server) {
      _server = server;
      _docLoader = new XmlDocumentationLoader();
    }

    public void BuildModel() {
      _model = _server.Model = new GraphQLApiModel(_server);
      _modelAdjustments = _server.Modules.SelectMany(m => m.Adjustments).ToList();

      // collect,register scalar, data types, query/mutation types, resolvers
      RegisterScalars(); 
      if (!RegisterGraphQLTypes())
        return;
      RegisterResolverClasses();

      if (!BuildRegisteredDirectiveDefinitions())
        return; 

      if (!AssignMappedEntitiesForObjectTypes())
        return;

      BuildTypesInternalsFromClrType();
      if (_model.HasErrors)
        return;

      LinkImplementedInterfaces();
      BuildUnionTypes();
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
      _model.ForEachModelObject(this.ApplyDirectives);
      if (_model.HasErrors)
        return;

      MapObjectFields();
      if (_model.HasErrors)
        return;

      var schemaGen = new SchemaDocGenerator();
      _model.SchemaDoc = schemaGen.GenerateSchema(_model);

      VerifyModel();
    }

    private void ApplyDirectives(GraphQLModelObject obj) {
      if (obj.Directives == null || obj.Directives.Count == 0)
        return;
      foreach (var dir in obj.Directives) {
        var action = dir.Def.Handler as IModelDirectiveAction;
        if (action == null)
          continue;
        action.Apply(_model, obj, dir.ModelAttribute.ArgValues);
      }
    }

    private void BuildTypesInternalsFromClrType() {
      foreach (var td in _model.Types) {
        if (td.ClrType == null)
          continue; // Special types (Query, Mutation etc) do not have Clr types
        DirectiveLocation loc = DirectiveLocation.None; 
        switch (td) {

          case InterfaceTypeDef intfTypeDef:
            loc = DirectiveLocation.Interface;
            BuildObjectTypeFields(intfTypeDef);
            break;

          case ObjectTypeDef objTypeDef:
            loc = DirectiveLocation.Object;
            BuildObjectTypeFields(objTypeDef);
            break;
          
          case InputObjectTypeDef inpTypeDef:
            loc = DirectiveLocation.InputObject;
            BuildInputObjectFields(inpTypeDef);
            break;
          
          case EnumTypeDef etd:
            loc = DirectiveLocation.Enum;
            // internal EnumHandler and enum values are already built in EnumTypeDef constructor
            break;
          
          case UnionTypeDef utd:
            loc = DirectiveLocation.Union;
            // we build union types in a separate loop after building other types
            break;
        } //switch
        td.Directives = BuildDirectivesFromAttributes(td.ClrType, loc, td);
      } //foreach td
    }

    // used for interface and Object types
    private void BuildObjectTypeFields(ComplexTypeDef typeDef) {
      var objTypeDef = typeDef as ObjectTypeDef;
      var clrType = typeDef.ClrType;
      var members = clrType.GetFieldsPropsMethods(withMethods: true);
      foreach (var member in members) {
        var attrs = GetAllAttributes(member);
        var ignoreAttr = attrs.Find<IgnoreAttribute>();
        if (ignoreAttr != null)
          continue;
        var mtype = member.GetMemberReturnType();
        var typeRef = GetTypeRef(mtype, member, $"Field {clrType.Name}.{member.Name}");
        if (typeRef == null)
          continue; //error should be logged already
        var name = GetGraphQLName(member);
        var descr = _docLoader.GetDocString(member, clrType);
        var fld = new FieldDef(typeDef, name, typeRef) { ClrMember = member, Description = descr, Attributes = attrs };
        fld.Directives = BuildDirectivesFromAttributes(member, DirectiveLocation.FieldDefinition, fld);
        if (attrs.Find<HiddenAttribute>() != null)
          fld.Flags |= FieldFlags.Hidden;
        typeDef.Fields.Add(fld);
        if (member is MethodInfo method)
          BuildFieldArguments(fld, method);
      }
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
        var attrs = GetAllAttributes(prm, method);
        var prmTypeRef = GetTypeRef(prm.ParameterType, prm, $"Method {method.Name}, parameter {prm.Name}", method);
        if (prmTypeRef == null)
          continue;
        if (prmTypeRef.IsList && !prmTypeRef.TypeDef.IsEnumFlagArray())
          VerifyListParameterType(prm.ParameterType, method, prm.Name);
        var dftValue = prm.DefaultValue == DBNull.Value ? null : prm.DefaultValue;
        var argDef = new InputValueDef() {
          Name = GetGraphQLName(prm), TypeRef = prmTypeRef, Attributes = attrs,
          ParamType = prm.ParameterType, HasDefaultValue = prm.HasDefaultValue, DefaultValue = dftValue
        };
        argDef.Directives = BuildDirectivesFromAttributes(prm, DirectiveLocation.ArgumentDefinition, argDef);
        argDefs.Add(argDef);
      }
      return argDefs; 
    }

    private void LinkImplementedInterfaces() {
      var objTypes = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object);
      foreach(var typeDef in objTypes) {
        if (typeDef.ClrType == null) //exclude Intro objects and special types
          continue;
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
        var attrs = GetAllAttributes(member); 
        var mtype = member.GetMemberReturnType();
        var typeRef = GetTypeRef(mtype, member, $"Field {inpTypeDef.Name}.{member.Name}");
        if (typeRef == null)
          return; // error found, it is already logged
        if (typeRef.IsList && !typeRef.TypeDef.IsEnumFlagArray()) {
          // list members must be IList<T> or T[] - this is important, lists are instantiated as arrays when deserializing
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
        var inpFldDef = new InputValueDef() { Name = member.Name.FirstLower(), TypeRef = typeRef, Attributes = attrs,
          InputObjectClrMember = member,
          Description = _docLoader.GetDocString(member, member.DeclaringType)
        };
        inpFldDef.Directives = BuildDirectivesFromAttributes(member, DirectiveLocation.InputFieldDefinition, inpFldDef);
        inpTypeDef.Fields.Add(inpFldDef);
      } //foreach
    }

    private void BuildUnionTypes() {
      var unionTypes = _model.Types.Where(td => td.Kind == TypeKind.Union).ToList();
      foreach(UnionTypeDef utDef in unionTypes) {
        var objTypes = utDef.ClrType.BaseType.GetGenericArguments();
        foreach(var objType in objTypes) {
          var typeDef = _model.LookupTypeDef(objType);
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
      _model.QueryType = BuildRootSchemaObject("Query", ObjectTypeRole.Query, ObjectTypeRole.ModuleQuery);
      _model.MutationType = BuildRootSchemaObject("Mutation", ObjectTypeRole.Mutation, ObjectTypeRole.ModuleMutation);
      _model.SubscriptionType = BuildRootSchemaObject("Subscription", ObjectTypeRole.Subscription, ObjectTypeRole.Subscription);

      if (_model.QueryType == null) {
        AddError("No fields are registered for Query root type; must have at least one query field.");
        return; 
      }
      var noAttrs = GraphQLModelObject.EmptyAttributeList;
      var schemaDef = _model.Schema = new ObjectTypeDef("Schema", null, noAttrs, null, ObjectTypeRole.Schema);
      RegisterTypeDef(schemaDef);
      schemaDef.Hidden = false; 
      schemaDef.Fields.Add(new FieldDef(schemaDef, "query", _model.QueryType.TypeRefNull));
      if (_model.MutationType != null)
        schemaDef.Fields.Add(new FieldDef(schemaDef, "mutation", _model.MutationType.TypeRefNull));
      if (_model.SubscriptionType != null)
        schemaDef.Fields.Add(new FieldDef(schemaDef, "subscription", _model.SubscriptionType.TypeRefNull));
    }

    private ObjectTypeDef BuildRootSchemaObject(string name, ObjectTypeRole typeRole, ObjectTypeRole moduleTypeRole) {
      var allFields = _model.Types.OfType<ObjectTypeDef>()
                        .Where(t => t.TypeRole == moduleTypeRole)
                        .SelectMany(t => t.Fields).ToList();
      if (allFields.Count == 0)
        return null;
      // TODO: add check for name duplicates
      var rootObj = new ObjectTypeDef(name, null, GraphQLModelObject.EmptyAttributeList, null, typeRole);
      rootObj.Fields.AddRange(allFields);
      RegisterTypeDef(rootObj);
      rootObj.Hidden = false; 
      return rootObj;
    }

    private void VerifyModel() {
    }



  } //class
}
