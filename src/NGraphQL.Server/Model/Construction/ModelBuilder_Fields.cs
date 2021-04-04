using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  public partial class ModelBuilder {


    IList<ObjectTypeDef> _typesToMapFields;

    private bool MapObjectFields() {
      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      _typesToMapFields = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
      // 1. Entity mapping expressions
      ProcessEntityMappingExpressions();
      if (_model.HasErrors) return false;

      // 2. Map by ResolvesField attribute on resolver methods
      MapResolversByResolvesFieldAttribute();
      if (_model.HasErrors) return false;

      // 3. to map by Resolver attribute on field
      MapFieldsToResolversByResolverAttribute();
      if (_model.HasErrors) return false;

      // 4. Default mapping to entities by name
      ProcessMappingForMatchingMembers();
      if (_model.HasErrors) return false;

      // 5. Map to resolvers by name 
      MapFieldsToResolversByName();
      if (_model.HasErrors) return false;

      // 6. Verify all assigned
      VerifyFieldMappings();

      return !_model.HasErrors;
    }

    private bool AssignMappedEntitiesForObjectTypes() {
      foreach (var module in _server.Modules) {
        var mname = module.GetType().Name;
        foreach (var mp in module.Mappings) {
          var typeDef = _model.LookupTypeDef(mp.GraphQLType);
          if (typeDef == null) {
            AddError($"Mapping target type {mp.GraphQLType.Name} is not registered; module {mname}");
            continue;
          }
          
          if (!(typeDef is ObjectTypeDef objTypeDef)) {
            AddError($"Invalid mapping target type {mp.GraphQLType.Name}, must be Object type; module {mname}");
            continue;
          }
          objTypeDef.Mapping = mp;
          _model.TypesByEntityType[mp.EntityType] = objTypeDef;
        }
      }
      // Self-mapped object types
      // if some GraphQL type is not mapped to anything, we assume it is mapped to itself. 
      // This is the case for introspection types, there are no entities behind them, 
      //  they are entities themselves. 
      //  Add this mappings explicitly, this will allow building field readers on each
      //  field definition. 
      foreach (var typeDef in _model.Types) {
        if (typeDef is ObjectTypeDef otd && otd.TypeRole == TypeRole.Data && otd.Mapping == null) {
          otd.Mapping = new ObjectTypeMapping() { EntityType = typeDef.ClrType, GraphQLType = typeDef.ClrType };
          _model.TypesByEntityType[typeDef.ClrType] = otd;
        }
      }
      return !_model.HasErrors;
    }

    private void VerifyFieldMappings() {
      foreach (var typeDef in _typesToMapFields) {
        foreach (var field in typeDef.Fields) {
          // so far we have only exec type to set, or post error
          if (field.Reader != null)
            field.ExecutionType = FieldExecutionType.Reader;
          else if (field.Resolver != null)
            field.ExecutionType = FieldExecutionType.Resolver;
          else
            AddError($"Field '{typeDef.ClrType.Name}.{field.Name}' (module {typeDef.Module.Name}) has no associated resolver or mapped entity field.");
        }
      }
    }

    private void ProcessEntityMappingExpressions() {
      foreach (var typeDef in _typesToMapFields) {
        var mapping = typeDef.Mapping;
        if (mapping?.Expression == null)
          continue; 
        var entityPrm = mapping.Expression.Parameters[0];
        var memberInit = mapping.Expression.Body as MemberInitExpression;
        if (memberInit == null) {
          AddError($"Invalid mapping expression for type {mapping.EntityType}->{mapping.GraphQLType.Name}");
          return;
        }
        foreach (var bnd in memberInit.Bindings) {
          var asmtBnd = bnd as MemberAssignment;
          var fieldDef = typeDef.Fields.FirstOrDefault(fld => fld.ClrMember == bnd.Member);
          if (asmtBnd == null || fieldDef == null)
            continue; //should never happen, but just in case
          // create lambda reading the source property
          fieldDef.Reader = CompileFieldReader(fieldDef, entityPrm, asmtBnd.Expression);
        }
      }
    }

    // those members that do not have binding expressions, try mapping props with the same name
    private void ProcessMappingForMatchingMembers() {
      foreach (var typeDef in _typesToMapFields) {
        var mapping = typeDef.Mapping;
        if (mapping == null)
          continue; 
        var entityType = mapping.EntityType;
        var allEntFldProps = entityType.GetFieldsProps();
        foreach (var fldDef in typeDef.Fields) {
          if (fldDef.Resolver != null || fldDef.Reader != null)
            continue;
          var memberName = fldDef.ClrMember.Name;
          MemberInfo entMember = allEntFldProps.Where(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
          if (entMember == null)
            continue;
          // TODO: maybe change reading to use compiled lambda 
          switch (entMember) {
            case FieldInfo fi:
              fldDef.Reader = (ent) => fi.GetValue(ent);
              break;
            case PropertyInfo pi:
              fldDef.Reader = (ent) => pi.GetValue(ent);
              break;
          }
        } //foreach fldDef
      }
    }

    private Func<object, object> CompileFieldReader(FieldDef field, ParameterExpression entityParam, Expression body) {
      
      // check if body is a MapTo func - return the source entity, mapping will be handled by the caller
      var methCall = body as MethodCallExpression;
      var usesFromMapFunc = methCall != null && methCall.Method.DeclaringType == typeof(GraphQLModule) &&
                             methCall.Method.Name == nameof(GraphQLModule.FromMap);
      if (usesFromMapFunc)
        body = methCall.Arguments[0];
      else
        field.Flags |= FieldFlags.ResolverReturnsGraphQLObject;
      // TODO: add return type validation
      var baseLambda = Expression.Lambda(body, entityParam);
      var newParentPrm = Expression.Parameter(typeof(object));
      var parentObj = Expression.Convert(newParentPrm, entityParam.Type);
      var invokeBaseLambdaExpr = Expression.Invoke(baseLambda, parentObj); 
      var convResultExpr = Expression.Convert(invokeBaseLambdaExpr, typeof(object));
      var newLambda = Expression.Lambda(convResultExpr, newParentPrm);
      var compiledLambda = newLambda.Compile();
      var func = (Func<object, object>)compiledLambda;
      return func; 
    }

  }
}
