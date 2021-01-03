using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  class ResolverMapper {
    #region nested types
    enum MappingType {
      None,
      ResolverAttr,
      ResolvesFieldAttr,
      ResolverByName,
      Expression,
      ExpressionByName
    }

    class FieldMapping {
      public FieldDef Field;
      public bool HasArgs;
      public MappingType MappingType;
      public ResolverAttribute ResolverAttr;
      public ResolverInfo Resolver;
      public GraphQLModule Module => Field.OwnerType.Module;
      public override string ToString() => Field.Name;
    }

    class ResolverInfo {
      public ResolverClassInfo ClassInfo;
      public MethodInfo Method;
      public ResolvesFieldAttribute ResolvesAttr;
      public override string ToString() => Method.Name;
    }
    #endregion

    GraphQLApiModel _model;
    List<ObjectTypeDef> _types;
    List<FieldMapping> _mappings;
    List<ResolverInfo> _allResolvers;

    public ResolverMapper(GraphQLApiModel model) {
      _model = model;
    }

    public void MapResolvers() {
      BuildInitialLists();
      MapResolversByResolvesFieldAttribute();
    } //method

    private void MapResolversByResolvesFieldAttribute() {
      var resInfos = _allResolvers.Where(r => r.ResolvesAttr != null).ToList(); 
      foreach(var res in resInfos) {
        var targetType = res.ResolvesAttr.TargetType;
        // check target type is valid
        if (targetType != null) {
          if (!_model.TypesByClrType.TryGetValue(targetType, out var typeDef) || !(typeDef is ObjectTypeDef objTypeDef)) {
            AddError($"Resolver method '{res.Type}.{res.Method.Name}': target type '{targetType}' not registered or "
                     + "is not Object type.");
            continue;
          }
        }
        // select fields by target type
        var mappingsToCheck = _mappings.Where(m => m.Field.OwnerType.ClrType == targetType || targetType == null).ToList();
        // map by name now
        var fname = res.ResolvesAttr.FieldName;
        var match = mappingsToCheck.Where(m => m.Field.Name == fname).ToList(); 
        switch(match.Count) {
          case 0:
            break;
          case 1:
            var mappiing = match[0];
            mappiing.MappingType = MappingType.ResolvesFieldAttr;
            mappiing.Resolver = res;
            break;
          default:
            break; 
        }
      }
    }


    private void BuildInitialLists() {
      // get all object types except root types (Query, Mutation, Schema) that do not have CLR type. 
      _types = _model.GetTypeDefs<ObjectTypeDef>(TypeKind.Object)
                              .Where(td => td.ClrType != null).ToList();
      _mappings = _types.SelectMany(t => t.Fields).Select(f => new FieldMapping() { Field = f }).ToList();
      _allResolvers = new List<ResolverInfo>();
      foreach (var rc in _model.Resolvers) {
        var methods = rc.Type.GetMethods();
        foreach (var meth in methods) {
          var attr = meth.GetAttribute<ResolvesFieldAttribute>();
          var resInfo = new ResolverInfo() { ClassInfo = rc, Method = meth, ResolvesAttr = attr };
          _allResolvers.Add(resInfo);
        }
      } //foreach rc

      // Validation of all atributes (ResolvesField and Resolver) - check target types are registered
      // validate type references from resolver methods
      var checkedTypes = new HashSet<Type>(); 
      foreach(var resInfo in _allResolvers) {
        var targetType = resInfo.ResolvesAttr?.TargetType;
        if (targetType == null || checkedTypes.Contains(targetType))
          continue;
        checkedTypes.Add(targetType); 
        if (!_model.TypesByClrType.TryGetValue(targetType, out var typeDef)) {
          AddError($"[ResolvesField] attribute on '{resInfo.ClassInfo.Type}.{resInfo.Method.Name}' method: " 
            + $"target type '{targetType}' is not registered.");
          continue;
        }
        if (!(typeDef is ObjectTypeDef)) {
          AddError($"[ResolvesField] attribute on '{resInfo.ClassInfo.Type}.{resInfo.Method.Name}' method: "
            + $"invalid target type '{targetType}', expected Object type kind.");
          continue;
        }
      } //foreach resInfo

      // validate target resolver classes
      checkedTypes.Clear(); 
      foreach(var fmap in _mappings) {
        var resType = fmap.ResolverAttr?.ResolverClass;
        
      }

    }

    public void AddError(string message) {
      _model.Errors.Add(message);
    }
  }
}
