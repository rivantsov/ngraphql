using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    public void BuildModel() {
      _model = _api.Model;
      // collect all data types, query/mutation types, resolvers
      if (!CollectRegisteredClrTypes())
        return;
      
      CollectResolverMethods();

      if (!AssignMappedEntitiesForObjectTypes())
        return; 
      // build root schema objects: query, mutation, subscription
      _model.QueryType = new ObjectTypeDef("Query", null) { TypeRole = SchemaTypeRole.Query };

      BuildTypesInternals();
      

    }




    private void BuildTypesInternals() {
      
      foreach (var td in _model.Types) {
        td.Directives = BuildDirectivesFromAttributes(td.ClrType);
        switch (td) {
          case ComplexTypeDef complexTypeDef:
            BuildObjectTypeFields(complexTypeDef);
            break;
          case InputObjectTypeDef itd:
            BuildInputObjectFields(itd);
            break;
          case EnumTypeDef etd:
            BuildEnumValues(etd);
            break;
          case UnionTypeDef utd:
            // we build union types in a separate loop after building other types
            break;
        } //switch
      } //foreach td
    }

    private void BuildObjectTypeFields(ComplexTypeDef typeDef) {
      var objTypeDef = typeDef as ObjectTypeDef;
      var clrType = typeDef.ClrType;
      var members = clrType.GetFieldsProps();
      foreach (var member in members) {
        var ignoreAttr = member.GetCustomAttribute<IgnoreAttribute>();
        if (ignoreAttr != null)
          continue;
        var mtype = member.GetMemberType();
        var typeRef = GetTypeRef(mtype, member, $"Field {clrType.Name}.{member.Name}");
        var dirs = BuildDirectivesFromAttributes(member);
        var name = GetGraphQLName(member);
        var descr = _docLoader.GetDocString(member, clrType);
        var fld = new FieldDef(name, typeRef) {
          ClrMember = member, Directives = dirs,
          Description = descr,
        };
        typeDef.Fields.Add(fld);
        if (objTypeDef != null)
          TryFindAssignFieldResolver(objTypeDef, fld);
      }
      // mapping expressions
      if (objTypeDef != null) {
        if (objTypeDef.Mapping?.Expression != null)
          ProcessEntityMappingExpression(objTypeDef);
        ProcessMappingForMatchingMembers(objTypeDef); 
      }
    }

    private bool AssignMappedEntitiesForObjectTypes() {
      foreach (var module in _api.Modules) {
        var mname = module.GetType().Name;
        foreach (var mp in module.Mappings) {
          var typeDef = _model.LookupTypeDef(mp.GraphQLType);
          if (typeDef == null) {
            AddError($"Mapping target type {mp.GraphQLType.Name} is not registered; module {mname}");
            continue; 
          }
          if(typeDef.TypeRole != SchemaTypeRole.DataType || typeDef.Kind != TypeKind.Object) {
            AddError($"Invalid mapping target type {mp.GraphQLType.Name}, expected data object type; module {mname}");
            continue;
          }
          var objTypeDef = (ObjectTypeDef)typeDef;
          objTypeDef.Mapping = mp; 
        }
      }
      // Self-mapped object types
      // if some GraphQL type is not mapped to anything, we assume it is mapped it itself. 
      // This is the case for introspection types, there are no entities behind them, 
      //  they are entities themselves. 
      //  Add this mappings explicitly, this will allow building field readers on each
      //  field definition. 
      foreach (var typeDef in _model.Types) {
        if (typeDef.TypeRole == SchemaTypeRole.DataType && typeDef is ObjectTypeDef otd && otd.Mapping == null) {
          otd.Mapping = new EntityMapping() { EntityType = typeDef.ClrType, GraphQLType = typeDef.ClrType };
        }
      }
      return !_model.HasErrors; 
    }

    private bool CollectRegisteredClrTypes() {
      foreach (var module in _api.Modules) {
        var mName = module.GetType().Name;
        foreach (var type in module.Types) {
          if (_model.TypesByClrType.ContainsKey(type)) {
            AddError($"Duplicate registration of type {type.Name}, module {mName}.");
            continue;
          }
          var roleAttrs = type.GetAttributes<GraphQLTypeRoleAttribute>();
          if (roleAttrs.Count > 1) {
            var strAttrs = string.Join(", ", roleAttrs.Select(a => a.GetType().Name));
            AddError($"Duplicate/incompatible attributes on type {type.Name}, module {mName}: {strAttrs}");
            continue;
          }
          var roleAttr = roleAttrs.FirstOrDefault();
          if (!ValidateTypeRoleKind(type, module, roleAttr, out var typeRole, out var typeKind))
            continue;
          var typeDef = CreateTypeDef(type, module, typeRole, typeKind);
          RegisterTypeDef(typeDef);
        } //foreach type
        // scalars and directives
        foreach (var scalar in module.Scalars)
          RegisterTypeDef(scalar);
        foreach (var dirDef in module.Directives)
          _model.Directives[dirDef.Name] = dirDef;
      } // foreach module
      return !_model.HasErrors;
    } //method


    private void RegisterTypeDef(TypeDefBase typeDef) {
      try {
        _model.Types.Add(typeDef);
        _model.TypesByName.Add(typeDef.Name, typeDef);
        if (typeDef.ClrType != null) {
          if (typeDef.Kind != TypeKind.Scalar)
            typeDef.Description = _docLoader.GetDocString(typeDef.ClrType, typeDef.ClrType);
          if (typeDef.IsDefaultForClrType)
            _model.TypesByClrType.Add(typeDef.ClrType, typeDef);
        }
      } catch (Exception ex) {
        AddError($"FATAL: Failed to register type {typeDef}, name '{typeDef.Name}', error: " + ex.Message);
      }
    }

    private TypeDefBase CreateTypeDef(Type type, GraphQLModule module, SchemaTypeRole typeRole, TypeKind typeKind) {
      var typeDef = CreateTypeDefImpl(type, typeKind);
      if (typeDef == null)
        return null;
      typeDef.Module = module;
      typeDef.TypeRole = typeRole; 
      var hideAttr = type.GetAttribute<HiddenAttribute>();
      if (hideAttr != null)
        typeDef.Hidden = true;
      return typeDef; 
    }

    private TypeDefBase CreateTypeDefImpl(Type type, TypeKind typeKind) {
      var typeName = GetGraphQLName(type);
      // Enum
      if (type.IsEnum) {
        var flagsAttr = type.GetAttribute<FlagsAttribute>();
        return new EnumTypeDef(typeName, type, isFlagSet: flagsAttr != null);
      }
      switch (typeKind) {
        case TypeKind.Object:
          return new ObjectTypeDef(typeName, type);
        case TypeKind.Interface:
          return new InterfaceTypeDef(typeName, type);
        case TypeKind.InputObject:
          return new InputObjectTypeDef(typeName, type);
        case TypeKind.Union:
          return new UnionTypeDef(typeName, type);
      }
      // should never happen
      return null;
    }

    private bool ValidateTypeRoleKind(Type clrType, GraphQLModule module, GraphQLTypeRoleAttribute typeRoleAttr, 
                                      out SchemaTypeRole typeRole, out TypeKind typeKind) {
      typeRole = default;
      typeKind = default;
      var errLoc = $"type {clrType.Name}, module {module.GetType().Name}";
      var isSpecialType = clrType.IsEnum || clrType.IsInterface || clrType.IsAssignableFrom(typeof(UnionBase));
      if (isSpecialType && typeRoleAttr != null) {
        AddError($"Attribute {typeRoleAttr.GetType().Name} is invalid on this type; {errLoc}");
        return false;
      }

      typeRole = typeRoleAttr == null ? SchemaTypeRole.DataType : typeRoleAttr.TypeRole;
      if (typeRole != SchemaTypeRole.DataType) {
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
          else if (clrType.IsAssignableFrom(typeof(UnionBase)))
            typeKind = TypeKind.Union;
          else {
            result = false;
            if (!clrType.IsClass) {
              AddError($"Invalid registered type, must be a class; {errLoc}");
            }
            AddError($"Registered type is missing attribute identifying GraphQL TypeKind; {errLoc}.");
          }
          break;

        default:
          result = false;
          break; 
      }
      return result; 
    }




  }//class

}
