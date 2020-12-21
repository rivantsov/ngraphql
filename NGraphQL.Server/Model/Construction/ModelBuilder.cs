using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Core.Scalars;
using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {
    GraphQLServer _server; 
    GraphQLApiModel _model;
    XmlDocumentationLoader _docLoader;

    public ModelBuilder(GraphQLServer server) {
      _server = server;
      _docLoader = new XmlDocumentationLoader();
    }

    public void BuildModel() {
      _model = _server.Model = new GraphQLApiModel(_server);

      // collect all data types, query/mutation types, resolvers
      if (!RegisterGraphQLTypesAndScalars())
        return;

      if (!BuildRegisteredDirectiveDefinitions())
        return; 

      if (!AssignMappedEntitiesForObjectTypes())
        return;

      BuildTypesInternals();
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

      var schemaGen = new SchemaDocGenerator();
      _model.SchemaDoc = schemaGen.GenerateSchema(_model);

      VerifyModel();

    }



    private void BuildTypesInternals() {

      foreach (var td in _model.Types) {
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
            BuildEnumValues(etd);
            break;
          case UnionTypeDef utd:
            loc = DirectiveLocation.Union;
            // we build union types in a separate loop after building other types
            break;
        } //switch
        td.Directives = BuildDirectivesFromAttributes(td.ClrType, loc, td);
      } //foreach td
    }

    private void BuildObjectTypeFields(ComplexTypeDef typeDef) {
      var objTypeDef = typeDef as ObjectTypeDef;
      var clrType = typeDef.ClrType;
      var members = clrType.GetFieldsPropsMethods();
      foreach (var member in members) {
        var ignoreAttr = member.GetCustomAttribute<IgnoreAttribute>();
        if (ignoreAttr != null)
          continue;
        var mtype = member.GetMemberReturnType();
        var typeRef = GetTypeRef(mtype, member, $"Field {clrType.Name}.{member.Name}");
        if (typeRef == null)
          continue; //error should be logged already
        var name = GetGraphQLName(member);
        var descr = _docLoader.GetDocString(member, clrType);
        var fld = new FieldDef(name, typeRef) { ClrMember = member, Description = descr  };
        fld.Directives = BuildDirectivesFromAttributes(member, DirectiveLocation.FieldDefinition, fld);
        if (member.HasAttribute<HiddenAttribute>())
          fld.Flags |= FieldFlags.Hidden;
        typeDef.Fields.Add(fld);
        if (member is MethodInfo method)
          BuildFieldArguments(fld, method);
        if (objTypeDef != null)
          TryFindAssignFieldResolver(objTypeDef, fld);
      }
      // mapping expressions
      if (objTypeDef?.Mapping != null) {
        if (objTypeDef.Mapping.Expression != null)
          ProcessEntityMappingExpression(objTypeDef);
        ProcessMappingForMatchingMembers(objTypeDef);
      }
    }

    private void BuildFieldArguments(FieldDef fieldDef, MethodInfo resMethod) {
      var prms = resMethod.GetParameters();
      if (prms == null || prms.Length == 0)
        return;
      foreach (var prm in prms) {
        var prmTypeRef = GetTypeRef(prm.ParameterType, prm, $"Method {resMethod.Name}, parameter {prm.Name}");
        if (prmTypeRef == null)
          continue; 
        if (prmTypeRef.IsList && !prmTypeRef.TypeDef.IsEnumFlagArray())
          VerifyListParameterType(prm.ParameterType, resMethod, prm.Name);
        var dftValue = prm.DefaultValue == DBNull.Value ? null : prm.DefaultValue;
        var argDef = new InputValueDef() {
          Name = GetGraphQLName(prm), TypeRef = prmTypeRef,
          ParamType = prm.ParameterType, HasDefaultValue = prm.HasDefaultValue, DefaultValue = dftValue};
        argDef.Directives = BuildDirectivesFromAttributes(prm, DirectiveLocation.ArgumentDefinition, argDef);
        fieldDef.Args.Add(argDef);
      }
    }

    public void AddError(string message) {
      _model.Errors.Add(message); 
    }

    private void LinkImplementedInterfaces() {
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
        var inpFldDef = new InputValueDef() { Name = member.Name.FirstLower(), TypeRef = typeRef, InputObjectClrMember = member,
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
        var enumV = new EnumValue() {
          Name = GetEnumFieldGraphQLName(fld), ClrValue = fldValue, ClrName = fldValue.ToString(), LongValue = longValue, 
          Description = _docLoader.GetDocString(fld, enumTypeDef.ClrType)
        };
        enumV.Directives = BuildDirectivesFromAttributes(fld, DirectiveLocation.EnumValue, enumV);
        enumTypeDef.EnumValues.Add(enumV);
      }
    }

    private void BuildSchemaDef() {
      _model.QueryType = BuildRootSchemaObject("Query", TypeRole.Query);
      _model.MutationType = BuildRootSchemaObject("Mutation", TypeRole.Mutation);
      _model.SubscriptionType = BuildRootSchemaObject("Subscription", TypeRole.Subscription);

      var schemaDef = _model.Schema = new ObjectTypeDef("Schema", null, _model.Server.CoreModule);
      RegisterTypeDef(schemaDef);
      schemaDef.Hidden = false; // RegisterTypeDef hides it unhide it
      // schemaDef.Hidden = false; // - leave it hidden; RegisterTypeDef sets it to true
      schemaDef.Fields.Add(new FieldDef("query", _model.QueryType.TypeRefNull));
      if (_model.MutationType != null)
        schemaDef.Fields.Add(new FieldDef("mutation", _model.MutationType.TypeRefNull));
      if (_model.SubscriptionType != null)
        schemaDef.Fields.Add(new FieldDef("subscription", _model.SubscriptionType.TypeRefNull));
    }

    private ObjectTypeDef BuildRootSchemaObject(string name, TypeRole typeRole) {
      var allFields = _model.Types.Where(t => t.TypeRole == typeRole)
          .Select(t => (ComplexTypeDef)t).SelectMany(t => t.Fields).ToList();
      if (allFields.Count == 0)
        return null;
      // TODO: add check for name duplicates
      var rootObj = new ObjectTypeDef(name, null, _model.Server.CoreModule) { TypeRole = typeRole };
      rootObj.Fields.AddRange(allFields);
      RegisterTypeDef(rootObj);
      rootObj.Hidden = false; 
      return rootObj;
    }

    private void VerifyModel() {
      foreach(var typeDef in _model.Types) {
        switch(typeDef) {
          case ObjectTypeDef otd:
              VerifyObjectType(otd); 
            break;
          case InputObjectTypeDef itd:
            break; 
        }
      }
    }

    private void VerifyObjectType(ObjectTypeDef typeDef) {
      if (typeDef.Name == "Schema")
        return; // this is pseudo object
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


    /*
    private bool ValidateTypeRoleKind(Type clrType, GraphQLModule module, GraphQLTypeRoleAttribute typeRoleAttr,
                                      out TypeRole typeRole, out TypeKind typeKind) {
      typeRole = default;
      typeKind = default;
      var errLoc = $"module {module.GetType().Name}";
      var isSpecialType = clrType.IsEnum || clrType.IsAssignableFrom(typeof(UnionBase));
      if (isSpecialType && typeRoleAttr != null) {
        AddError($"Attribute {typeRoleAttr.GetType().Name} is invalid on type {clrType}; {errLoc}");
        return false;
      }

      typeRole = typeRoleAttr == null ? TypeRole.DataType : typeRoleAttr.TypeRole;
      if (typeRole != TypeRole.DataType) {
        typeKind = TypeKind.Object;
        return true;
      }

      bool result = true;
      switch (typeRoleAttr) {
        case ObjectTypeAttribute _:
          typeKind = TypeKind.Object;
          break;
        case InputTypeAttribute _:
          typeKind = TypeKind.InputObject;
          break;

        case null:
          if (clrType.IsEnum)
            typeKind = TypeKind.Enum;
          else if (clrType.IsInterface)
            typeKind = TypeKind.Interface;
          else if (typeof(UnionBase).IsAssignableFrom(clrType))
            typeKind = TypeKind.Union;
          else {
            result = false;
            if (!clrType.IsClass) {
              AddError($"Invalid registered type  {clrType.Name}, must be a class; {errLoc}");
            }
            AddError($"Registered type {clrType.Name} is missing an attribute identifying its role (ObjectType, Query, etc); {errLoc}.");
          }
          break;

        default:
          result = false;
          break;
      }
      return result;
    }
    */


  } //class
}
