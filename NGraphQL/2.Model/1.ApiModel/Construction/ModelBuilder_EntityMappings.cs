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

    private void ProcessEntityMappingExpression(ObjectTypeDef typeDef) {
      var mapping = typeDef.Mapping; 
      var entityPrm = mapping.Expression.Parameters[0];
      var memberInit = mapping.Expression.Body as MemberInitExpression;
      if(memberInit == null) {
        AddError($"Invalid mapping expression for type {mapping.EntityType}->{mapping.GraphQLType.Name}");
        return;
      }
      foreach(var bnd in memberInit.Bindings) {
        var asmtBnd = bnd as MemberAssignment;
        var fieldDef = typeDef.Fields.FirstOrDefault(fld => fld.ClrMember == bnd.Member);
        if(asmtBnd == null || fieldDef == null)
          continue; //should never happen, but just in case
        // create lambda reading the source property
        fieldDef.Reader = CompileFieldReader(entityPrm, asmtBnd.Expression);
      }
    }

    // those members that do not have binding expressions, try mapping props with the same name
    private void ProcessMappingForMatchingMembers(ObjectTypeDef typeDef) {
      var mapping = typeDef.Mapping;
      var entityType = mapping.EntityType; 
      foreach(var fldDef in typeDef.Fields) {
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
