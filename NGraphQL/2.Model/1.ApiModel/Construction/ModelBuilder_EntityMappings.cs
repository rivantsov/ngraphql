using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  public partial class ModelBuilder {

    private void CollectEntityMappings() {
      var mappings = _api.Modules.SelectMany(m => m.Mappings).ToList();
      foreach(var mp in mappings) {
        mp.TypeDef = (ObjectTypeDef)_model.LookupTypeDef(mp.GraphQLType);
        _model.EntityMappings.Add(mp.EntityType, mp);
      }
    }

    // if some GraphQL type is not mapped to anything, we assume it is mapped it itself. 
    // This is the case for introspection types, there are no entities behind them, 
    //  they are entities themselves. 
    //  Add this mappings explicitly, this will allow building field readers on each
    //  field definition. 
    private void AddSelfMappedTypeMappings() {
      var mappedGqlTypes = _model.EntityMappings.Values.Select(m => m.GraphQLType);
      var mappedTypeSet = new HashSet<Type>(mappedGqlTypes);
      var notMappedTypes = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                           // Schema, Query defs have no CLR type
                           .Where(td => td.ClrType != null && !mappedTypeSet.Contains(td.ClrType)) 
                           .ToList();
      foreach(var td in notMappedTypes) {
        var type = td.ClrType;
        var mapping = new EntityMapping() { EntityType = type, GraphQLType = type, TypeDef = td };
        _model.EntityMappings.Add(type, mapping);
      }
    }

    // Anaylyze mapping expression and split it into expressions copying each property; 
    //  then compile them and attach to field definitions
    private void ProcessEntityMappings() {
      
      // actually process mappings
      foreach(var mapping in _model.EntityMappings.Values) {
        if (mapping.Expression != null)
          ProcessEntityMappingExpression(mapping);
        ProcessMappingForMatchingMembers(mapping); 
      }
    } //method

    private void ProcessEntityMappingExpression(EntityMapping mapping) {
      var entityPrm = mapping.Expression.Parameters[0];
      var memberInit = mapping.Expression.Body as MemberInitExpression;
      if(memberInit == null) {
        AddError($"Invalid mapping expression for type {mapping.EntityType}->{mapping.TypeDef.Name}");
        return;
      }
      foreach(var bnd in memberInit.Bindings) {
        var asmtBnd = bnd as MemberAssignment;
        var fieldDef = mapping.TypeDef.Fields.FirstOrDefault(fld => fld.ClrMember == bnd.Member);
        if(asmtBnd == null || fieldDef == null)
          continue; //should never happen, but just in case
        // create lambda reading the source property
        fieldDef.Reader = CompileFieldReader(entityPrm, asmtBnd.Expression);
      }
    }

    // those members that do not have binding expressions, try mapping props with the same name
    private void ProcessMappingForMatchingMembers(EntityMapping mapping) {
      var entityType = mapping.EntityType; 
      foreach(var fldDef in mapping.TypeDef.Fields) {
        if(fldDef.Resolver != null || fldDef.Reader != null)
          continue;
        var memberName = fldDef.ClrMember.Name;
        MemberInfo entMember = entityType.GetFieldsProps()
          .Where(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();
        if(entMember == null)
          continue;
        // TODO: maybe change reading to use compiled lambda 
        switch(entMember) {
          case FieldInfo fi:
            fldDef.Reader = (ent) => fi.GetValue(ent);
            break;
          case PropertyInfo pi:
            fldDef.Reader = (ent) => pi.GetValue(ent);
            break; 
        }
      } //foreach fldDef
    }

    private Func<object, object> CompileFieldReader( ParameterExpression entityParam, Expression body) {
      // check if body is a MapTo func - return the source entity, mapping will be handled by the caller
      if (body is MethodCallExpression mc && mc.Method.DeclaringType == typeof(GraphQLModule) && 
                             mc.Method.Name == nameof(GraphQLModule.FromMap) ) {
        body = mc.Arguments[0];
      }
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
