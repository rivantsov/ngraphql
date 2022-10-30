using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  public partial class ModelBuilder {

    private bool AssignObjectFieldResolvers() {
      if (_model.HasErrors) 
        return false;

      // create all resolvers in all mappings
      InitFieldResolvers();

      // get all object and input types except root types (Query, Mutation, Schema) that do not have CLR type. 
      var objectTypes = _model.Types
            .Where(td => td.Kind == TypeKind.Object || td.Kind == TypeKind.InputObject)
            .Where(td => td.ClrType != null)
            .Select(td => (ObjectTypeDef)td)
            .ToList();        
        
      // 1. Map by ResolvesField attribute on resolver methods
      AssignResolversByMethodsResolvesFieldAttribute(objectTypes);

      // from now on, iterate on types, mappings and setup resolvers for each mapping
      foreach (var typeDef in objectTypes) {
        foreach (var mapping in typeDef.Mappings) {
          // 2. Compile mapping expressions
          AssignResolversFromCompiledMappingExpressions(mapping);

          // 3. to map by Resolver attribute on field
          AssignResolversByFieldResolverAttribute(mapping);

          // 4. Default mapping to entities by name
          AssignResolversByEntityPropertyNameMatch(mapping);

          // 5. Map to resolvers by name 
          AssignResolversToMatchingResolverMethods(mapping);

          // 7. Verify all assigned
          VerifyAllResolversAssigned(mapping);

          if (_model.Errors.Count > 20)
            return false; // cut off at 20
        }
      }
      return !_model.HasErrors;
}
    private void InitFieldResolvers() {
      // Note: input objects also can be used as output objects
      var objTypes = _model.Types.Where(tdef => tdef.Kind == TypeKind.Object || tdef.Kind == TypeKind.InputObject)
                      .OfType<ObjectTypeDef>(); 
      foreach(var typeDef in objTypes)
        foreach(var mapping in typeDef.Mappings) {
          foreach(var fldDef in typeDef.Fields) { 
            var fres = new FieldResolverInfo() { Field = fldDef, TypeMapping = mapping };
            mapping.FieldResolvers.Add(fres); 
          }
        }
    }

    // setup resolvers having [ResolvesField] attribute
    private void AssignResolversByMethodsResolvesFieldAttribute(IList<ObjectTypeDef> types) {
      // go thru resolver classes, find methods with ResolvesField attr
      foreach (var resMethInfo in _allResolverMethods) {
        var resAttr = resMethInfo.ResolvesAttribute;
        if (resAttr == null)
          continue;
        var module = resMethInfo.Module;
        var fieldName = resAttr.FieldName.FirstLower();
        FieldDef field = null;
        var typeDefs = types.Where(t => t.Module == resMethInfo.Module).OfType<ObjectTypeDef>();
        // check target type
        if (resAttr.TargetType != null) {
          var typeDef = typeDefs.FirstOrDefault(td => td.ClrType == resAttr.TargetType);
          if (typeDef == null) {
            AddError($"Resolver method '{resMethInfo}': target type '{resAttr.TargetType}' not registered or is not Object type.");
            continue;
          }
          // match field
          if (!typeDef.Fields.TryGetValue(fieldName, out field))  {
            AddError($"Resolver method '{resMethInfo}': target field '{fieldName}' not found "
                      + $"on type '{resAttr.TargetType}'.");
            continue;
          }
        } else {
          // TargetType is null - find match by name only
          var fields = typeDefs.SelectMany(t => t.Fields).Where(f => f.Name == fieldName).ToList();
          switch (fields.Count) {
            case 1:
              field = fields[0];
              break;
            case 0:
              AddError($"Resolver method '{resMethInfo}': target field '{fieldName}' not found on any object type.");
              continue; 
            default:
              AddError($"Resolver method '{resMethInfo}': multipe target fields '{fieldName}' found on Object types.");
              continue;
          }
        }
        // We have a field; verify method is compatible
        VerifyFieldResolverMethod(field, resMethInfo);
        // get parent arg type and find mapping
        var objTypeDef = (ObjectTypeDef)field.OwnerType;
        ObjectTypeMapping mapping = null; 
        switch(objTypeDef.TypeRole) {
          
          case TypeRole.ModuleQuery: case TypeRole.ModuleMutation: case TypeRole.ModuleSubscription:
            mapping = objTypeDef.Mappings[0]; 
            break;
          
          case TypeRole.Data:
            var prms = resMethInfo.Method.GetParameters();
            if (prms.Length < 2) {
              AddError($"Resolver method '{resMethInfo}', expected at least 2 parameters - field context and parent entity.");
              continue; 
            }
            var parentEntType = prms[1].ParameterType;
            mapping = objTypeDef.Mappings.FirstOrDefault(m => m.EntityType.IsAssignableFrom(parentEntType));
            if (mapping == null) {
              AddError($"Resolver method '{resMethInfo}', parent entity argument type '{parentEntType}' is not mapped to output GraphQL type '{objTypeDef.Name}'.");
              continue; 
            }
            break;

          default:
            AddError($"Resolver method '{resMethInfo}', invalid target GraphQL type: '{objTypeDef.Name}'.");
            continue;
        }//switch
        var fldResolver = mapping.GetResolver(field);
        if (fldResolver == null) {
          AddError($"Resolver method '{resMethInfo}', failed to match to field resolver in type '{objTypeDef.Name}'.");
          continue; 
        }
        if (fldResolver.ResolverMethod != null) {
          AddError($"Field '{fldResolver.Field}': more than one resolver method specified.");
          continue;
        }
        fldResolver.ResolverMethod = resMethInfo; //assign resolver
      } //foreach resMeth
    } //method

    private void AssignResolversFromCompiledMappingExpressions(ObjectTypeMapping mapping) {
      if (mapping.Expression == null)
        return;
      var entityPrm = mapping.Expression.Parameters[0];
      var memberInit = mapping.Expression.Body as MemberInitExpression;
      if (memberInit == null) {
        AddError($"Invalid mapping expression for type '{mapping.EntityType}->{mapping.TypeDef.Name}'.");
        return;
      }
      foreach (var bnd in memberInit.Bindings) {
        var asmtBnd = bnd as MemberAssignment;
        if (asmtBnd == null) {
          AddError($"Invalid mapping expression '{bnd}', expected assignment binding.");
          continue;
        }
        var fldName = bnd.Member.Name.FirstLower();
        var fieldDef = mapping.TypeDef.Fields[fldName]; 
        if(fieldDef == null) {
          AddError($"Invalid assignment expression, target field '{fldName}' not found.");
          continue; 
        }
        // create lambda reading the source property
        var resInfo = mapping.GetResolver(fieldDef);
        if (resInfo == null)
          continue;
        if (resInfo.IsMapped()) {
          AddError($"Resolver mapper by LINQ expression: field '{fieldDef}' is already mapped to a resolver.");
          continue;
        }
        resInfo.ResolverFunc = CompileResolverExpression(entityPrm, asmtBnd.Expression);
      } //foreach bnd
    }

    private void AssignResolversByFieldResolverAttribute(ObjectTypeMapping mapping) {
      var typeDef = mapping.TypeDef;
      foreach (var field in typeDef.Fields) {
        if (field.ClrMember == null)
          continue; //__typename has no clr member
        var resAttr = GetAllAttributesAndAdjustments(field.ClrMember).Find<ResolverAttribute>();
        if (resAttr == null)
          continue;
        var resolverType = resAttr.ResolverClass;
        if (resolverType != null) {
          if (!typeDef.Module.ResolverClasses.Contains(resolverType)) {
            AddError($"Field {field.FullRefName}: target resolver class {resolverType.Name} is not registered with module. ");
            continue;
          }
        }
        // Get field res info and check if it's already mapped
        var methName = resAttr.MethodName ?? field.ClrMember.Name;
        var resMethInfos = FindResolvers(methName, resolverType);
        switch (resMethInfos.Count) {
          case 1:
            break;
          case 0:
            AddError($"Field {field.FullRefName}: failed to find resolver method {methName}. ");
            continue; //next field
          default:
            AddError($"Field {field.FullRefName}: found more than one resolver method ({methName}).");
            continue; //next field
        }
        // we have single matching resolver
        var resMethInfo = resMethInfos[0];
        VerifyFieldResolverMethod(field, resMethInfo);
        // get field resolver info and check if it is already mapped
        var fldRes = mapping.GetResolver(field);
        if (fldRes.IsMapped()) {
          AddError($"Field {field.FullRefName}: failed to set resolver '{methName}', field is already mapped. ");
          continue; //next field
        }
        fldRes.ResolverMethod = resMethInfo; 
      } //foreach field
    }//method

    // we try to match field defined as method, by member/method name, even if GraphQL name is different; match: GraphQL ObjType.Member => Resolver.Member
    private void AssignResolversToMatchingResolverMethods(ObjectTypeMapping mapping) {
      var typeDef = mapping.TypeDef; 
      foreach (var field in typeDef.Fields) {
        var fRes = mapping.GetResolver(field);
        if (fRes.IsMapped())
          continue;
        if (field.ClrMember == null)
          continue; //__typename has no clr member
        var methName = field.ClrMember.Name;
        var resolverInfos =  _allResolverMethods
          .Where(res => res.Module == mapping.TypeDef.Module) // in the same module!
          .Where(res =>  res.Method.Name.Equals(methName, StringComparison.OrdinalIgnoreCase)).ToList();
        switch (resolverInfos.Count) {
          case 0: 
            continue; // no match
          case 1:
            break;
          default:
            AddError($"Field {field.FullRefName}: found more than one resolver method ({methName}).");
            continue;
        } //switch
        var resInfo = resolverInfos[0];
        VerifyFieldResolverMethod(field, resInfo);
        var fldRes = mapping.GetResolver(field);
        fldRes.ResolverMethod = resInfo;
      } //foreach field
    }//method

    // those members that do not have binding expressions, try mapping props with the same name
    private void AssignResolversByEntityPropertyNameMatch(ObjectTypeMapping mapping) {
      var entityType = mapping.EntityType;
      var allEntFldProps = entityType.GetFieldsProps();
      foreach (var fldDef in mapping.TypeDef.Fields) {
        var res = mapping.GetResolver(fldDef);
        if (res.IsMapped())
          continue; //already set
        var memberName = fldDef.Name;
        MemberInfo entMember = allEntFldProps.Where(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();
        if (entMember == null)
          continue;
        res.ResolverFunc = ExpressionHelper.CompileMemberReader(entMember);
      } //foreach fldDef
    }

  }
}
