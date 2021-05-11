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


    IList<ObjectTypeDef> _typesToAssignResolvers;

    private bool AssignObjectFieldResolvers() {
      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      _typesToAssignResolvers = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
      // 1. Compile mapping expressions
      AssignResolversFromCompiledMappingExpressions();
      if (_model.HasErrors) return false;

      // 2. Map by ResolvesField attribute on resolver methods
      AssignResolversByMethodsResolvesFieldAttribute();
      if (_model.HasErrors) return false;

      // 3. to map by Resolver attribute on field
      AssignResolversByFieldResolverAttribute();
      if (_model.HasErrors) return false;

      // 4. Default mapping to entities by name
      AssignResolversByEntityPropertyNameMatch();
      if (_model.HasErrors) return false;

      // 5. Map to resolvers by name 
      AssignResolversToMatchingResolverMethods();
      if (_model.HasErrors) return false;

      // 6. Verify all assigned
      VerifyAllResolversAssigned();

      return !_model.HasErrors;
    }

    // setup resolvers having [ResolvesField] attribute
    private void AssignResolversByMethodsResolvesFieldAttribute() {
      // go thru resolver classes, find methods with ResolvesField attr
      foreach (var resInfo in _allResolvers) {
        var resAttr = resInfo.ResolvesAttribute;
        if (resAttr == null)
          continue;
        var fieldName = resAttr.FieldName.FirstLower();
        // check target type
        if (resAttr.TargetType != null) {
          if (!_model.TypesByClrType.TryGetValue(resAttr.TargetType, out var typeDef) || !(typeDef is ObjectTypeDef objTypeDef)) {
            AddError($"Resolver method '{resInfo}': target type '{resAttr.TargetType}' not registered or is not Object type.");
            continue;
          }
          // match field
          var fld = objTypeDef.Fields.FirstOrDefault(f => f.Name == fieldName);
          if (fld == null) {
            AddError($"Resolver method '{resInfo}': target field '{fieldName}' not found "
                      + $"on type '{resAttr.TargetType}'.");
            continue;
          }
          SetupFieldResolverMethod(objTypeDef, fld, resInfo, resAttr);
          continue;
        } // if TargetType != null
        // TargetType is null - find match by name only
        var fields = _typesToAssignResolvers.SelectMany(t => t.Fields).Where(f => f.Name == fieldName).ToList();
        switch (fields.Count) {
          case 1:
            var fld = fields[0];
            SetupFieldResolverMethod((ObjectTypeDef)fld.OwnerType, fld, resInfo, resAttr);
            break;

          case 0:
            AddError($"Resolver method '{resInfo}': target field '{fieldName}' not found "
                      + $"on any object type.");
            break;

          default:
            AddError($"Resolver method '{resInfo}': multipe target fields '{fieldName}' "
                      + $"found on Object types.");
            break;
        }
      } //foreach resMeth
    }


    private void AssignResolversFromCompiledMappingExpressions() {
      foreach (var typeDef in _typesToAssignResolvers) {
        foreach (var mapping in typeDef.Mappings) {
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
            var resFunc = CompileResolverExpression(fieldDef, entityPrm, asmtBnd.Expression);
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

    // those members that do not have binding expressions, try mapping props with the same name
    private void AssignResolversByEntityPropertyNameMatch() {
      foreach (var typeDef in _typesToAssignResolvers) {
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

    private void AssignResolversByFieldResolverAttribute() {
      foreach (var typeDef in _typesToAssignResolvers) {
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
          List<MethodInfo> methods;
          if (resolverType != null) {
            methods = resolverType.GetResolverMethods(methName);
            // with explicit resolver, if method not found - it is error
            if (methods.Count == 0) {
              AddError($"Field {typeDef.Name}.{field.Name}: failed to match resolver method; target resolver class {resolverType.Name}.");
              continue;
            }
          } else {
            // targetResolver is null
            methods = new List<MethodInfo>();
            foreach (var resType in typeDef.Module.ResolverClasses) {
              var mlist = resType.GetResolverMethods(methName);
              methods.AddRange(mlist);
            }
          }
          switch (methods.Count) {
            case 0:
              AddError($"Field {typeDef.Name}.{field.Name}: failed to find resolver method {methName}. ");
              break;

            case 1:
              SetupFieldResolverMethod(typeDef, field, methods[0], resAttr);
              break;

            default:
              AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
              break;
          }
        } //foreach field
      } //foreach typeDef
    }//method

    private void AssignResolversToMatchingResolverMethods() {
      foreach (var typeDef in _typesToAssignResolvers) {
        // get all resolver methods from the same module
        foreach (var field in typeDef.Fields) {
          if (field.ExecutionType != ResolverKind.NotSet)
            continue;
          if (field.ClrMember == null)
            continue; //__typename has no clr member
          var methName = field.ClrMember.Name;
          var methods = _allResolvers.Where(res => res.Method.Name == methName).ToList();
          switch (methods.Count) {
            case 0: continue;
            case 1:
              SetupFieldResolverMethod(typeDef, field, methods[0], null);
              continue;
            default:
              AddError($"Field {typeDef.Name}.{field.Name}: found more than one resolver method ({methName}).");
              continue;
          } //switch
        } //foreach field
      } //foreach typeDef
    }//method

  }
}
