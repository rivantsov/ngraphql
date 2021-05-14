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

      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      var objectTypes = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
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
      var objTypes = _model.Types.Where(tdef => tdef.Kind == TypeKind.Object).OfType<ObjectTypeDef>(); 
      foreach(var typeDef in objTypes)
        foreach(var mapping in typeDef.Mappings) {
          var resolvers = typeDef.Fields.Select(f => new FieldResolverInfo() { Field = f, TypeMapping = mapping }).ToList();
          mapping.FieldResolvers.AddRange(resolvers); 
        }
    }

    // setup resolvers having [ResolvesField] attribute
    private void AssignResolversByMethodsResolvesFieldAttribute(IList<ObjectTypeDef> types) {
      // go thru resolver classes, find methods with ResolvesField attr
      foreach (var resInfo in _allResolvers) {
        var resAttr = resInfo.ResolvesAttribute;
        if (resAttr == null)
          continue;
        var fieldName = resAttr.FieldName.FirstLower();
        FieldDef field = null;
        // check target type
        if (resAttr.TargetType != null) {
          if (!_model.TypesByClrType.TryGetValue(resAttr.TargetType, out var typeDef) || !(typeDef is ObjectTypeDef objTypeDef)) {
            AddError($"Resolver method '{resInfo}': target type '{resAttr.TargetType}' not registered or is not Object type.");
            continue;
          }
          // match field
          field = objTypeDef.Fields.FirstOrDefault(f => f.Name == fieldName);
          if (field == null) {
            AddError($"Resolver method '{resInfo}': target field '{fieldName}' not found "
                      + $"on type '{resAttr.TargetType}'.");
            continue;
          }
        } else {
          // TargetType is null - find match by name only
          var fields = types.SelectMany(t => t.Fields).Where(f => f.Name == fieldName).ToList();
          switch (fields.Count) {
            case 1:
              field = fields[0];
              break;
            case 0:
              AddError($"Resolver method '{resInfo}': target field '{fieldName}' not found on any object type.");
              continue; 
            default:
              AddError($"Resolver method '{resInfo}': multipe target fields '{fieldName}' found on Object types.");
              continue;
          }
        }
        // We have a field; verify method is compatible
        VerifyFieldResolverMethod(field, resInfo);
        // get parent arg type and find mapping

      } //foreach resMeth
    } //method


    private void AssignResolversFromCompiledMappingExpressions(ObjectTypeMapping mapping) {
        if (mapping.Expression == null)
          return;
        var entityPrm = mapping.Expression.Parameters[0];
        var memberInit = mapping.Expression.Body as MemberInitExpression;
        if (memberInit == null) {
          AddError($"Invalid mapping expression for type {mapping.EntityType}->{mapping.TypeDef.Name}");
          return;
        }
        foreach (var bnd in memberInit.Bindings) {
          var asmtBnd = bnd as MemberAssignment;
          var fieldDef = mapping.TypeDef.Fields.FirstOrDefault(fld => fld.ClrMember == bnd.Member);
          if (asmtBnd == null || fieldDef == null)
            continue; //should never happen, but just in case
                      // create lambda reading the source property
          var resFunc = CompileResolverExpression(fieldDef, entityPrm, asmtBnd.Expression);
          var outType = asmtBnd.Expression.Type;
          var resInfo = new FieldResolverInfo() {
            Field = fieldDef, ResolverFunc = resFunc,
            OutType = outType, TypeMapping = mapping
          };
          mapping.FieldResolvers.Add(resInfo);
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
            AddError($"Field {typeDef.Name}.{field.Name}: target resolver class {resolverType.Name} is not registered with module. ");
            continue;
          }
        }
        // 
        var methName = resAttr.MethodName ?? field.ClrMember.Name;
        var resolverInfos = FindResolvers(methName, resolverType);
        switch (resolverInfos.Count) {
          case 1:
            break;
          case 0:
            AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}. ");
            continue; //next field
          default:
            AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
            continue; //next field
        }
        // we have single matching resolver
        var resInfo = resolverInfos[0];
        VerifyFieldResolverMethod(field, resInfo);
        var fldRes = mapping.GetResolver(field);
        fldRes.ResolverMethod = resInfo; 
      } //foreach field
    }//method

    private void AssignResolversToMatchingResolverMethods(ObjectTypeMapping mapping) {
      var typeDef = mapping.TypeDef; 
      foreach (var field in typeDef.Fields) {
        var fRes = mapping.FieldResolvers.FirstOrDefault(fr => fr.Field == field);
        if (fRes != null)
          continue;
        if (field.ClrMember == null)
          continue; //__typename has no clr member
        var methName = field.Name; //.ClrMember.Name;
        var resolverInfos =  _allResolvers.Where(res => res.Method.Name.Equals(methName, StringComparison.OrdinalIgnoreCase)).ToList();
        switch (resolverInfos.Count) {
          case 0: 
            continue; // no match
          case 1:
            break;
          default:
            AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
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
        var res = mapping.FieldResolvers.FirstOrDefault(fr => fr.Field == fldDef);
        Util.Check(res != null, $"FATAL: resolver for field {fldDef.Name}, type {fldDef.TypeRef.TypeDef.Name} not created. ");
        if (res.ResolverFunc != null || res.ResolverMethod != null)
          continue; //already set
        var memberName = fldDef.Name;
        MemberInfo entMember = allEntFldProps.Where(m => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();
        if (entMember == null)
          continue;
        // TODO: maybe change reading to use compiled lambda 
        switch (entMember) {
          case FieldInfo fi:
            res.ResolverFunc = (ent) => fi.GetValue(ent);
            break;
          case PropertyInfo pi:
            res.ResolverFunc = (ent) => pi.GetValue(ent);
            break;
          default:
            continue; // we consider it no match 
        }
      } //foreach fldDef
    }

  }
}
