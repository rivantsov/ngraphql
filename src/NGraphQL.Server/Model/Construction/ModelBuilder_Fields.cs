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
          var mappingExt = new ObjectTypeMappingExt(mp);
          objTypeDef.Mappings.Add(mappingExt);
          _model.TypesByEntityType[mp.EntityType] = objTypeDef;
        }
      }
      // Add self-maps to all objects
      foreach (var typeDef in _model.Types) {
        if (typeDef is ObjectTypeDef otd && otd.TypeRole == TypeRole.Data) {
          var mapping = new ObjectTypeMappingExt(otd.ClrType);
          otd.Mappings.Add(mapping);
        }
      }
      return !_model.HasErrors;
    }

    private void VerifyFieldMappings() {
      foreach (var typeDef in _typesToMapFields) {
        foreach(var mapping in typeDef.Mappings) {
          foreach (var fres in mapping.FieldResolvers) {
            // so far we have only exec type to set, or post error
            if (fres.ResolverFunc != null)
              fres.ResolverKind = ResolverKind.CompiledExpression;
            else if (fres.ResolverMethod != null)
              fres.ResolverKind = ResolverKind.Method;
            else {
              var fldName = fres.Field.Name;
              var fldRef = $"{typeDef.ClrType.Name}.{fldName}, mapping to {mapping.EntityType}, " + 
                $" (module {typeDef.Module.Name})";
              AddError($"Field {fldName} has no associated resolver or mapped entity field. Field: {fldRef}");
            }
          } // foreach fres
        } // foreach mapping
      } // foreach typeDef
    }

    private void ProcessEntityMappingExpressions() {
      foreach (var typeDef in _typesToMapFields) {
        foreach(var mapping in typeDef.Mappings) {
          if (mapping.Expression == null)
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
            var resFunc = CompileFieldReader(fieldDef, entityPrm, asmtBnd.Expression);
            var outType = asmtBnd.Expression.Type;
            var resInfo = new FieldResolverInfo() {
              Field = fieldDef, ResolverFunc = resFunc,
              OutType = outType, TypeMapping = mapping
            };
            mapping.FieldResolvers.Add(resInfo);
          } //foreach bnd
        } //foreach mapping
      } //foreach typeDef
    }

    // those members that do not have binding expressions, try mapping props with the same name
    private void ProcessMappingForMatchingMembers() {
      foreach (var typeDef in _typesToMapFields) {
        foreach(var mapping in typeDef.Mappings) {
          var entityType = mapping.EntityType;
          var allEntFldProps = entityType.GetFieldsProps();
          foreach (var fldDef in typeDef.Fields) {
            var res = mapping.FieldResolvers.FirstOrDefault(fr => fr.Field == fldDef);
            if (res != null)
              continue;
            var memberName = fldDef.ClrMember.Name;
            MemberInfo entMember = allEntFldProps.Where(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();
            if (entMember == null)
              continue;
            // TODO: maybe change reading to use compiled lambda 
            Func<object, object> resFunc = null;
            switch (entMember) {
              case FieldInfo fi:
                resFunc = (ent) => fi.GetValue(ent);
                break;
              case PropertyInfo pi:
                resFunc = (ent) => pi.GetValue(ent);
                break;
              default:
                continue; // we consider it no match 
            }
            var fldRes = new FieldResolverInfo() { Field = fldDef,  OutType = entMember.GetMemberReturnType(), 
                ResolverKind = ResolverKind.Func, ResolverFunc = resFunc };
            mapping.FieldResolvers.Add(fldRes); 
          } //foreach fldDef
        } // foreach mapping
      } // foreach typeDef
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
