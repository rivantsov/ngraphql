using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public static partial class ModelExtensions {

    public static bool IsSet(this FieldFlags flags, FieldFlags flag) {
      return (flags & flag) != 0; 
    }

    public static bool IsSet(this DirectiveLocation locs, DirectiveLocation loc) {
      return (locs & loc) != 0;
    }

    public static int GetListRank(this IList<TypeKind> kinds) {
      return kinds.Count(k => k == TypeKind.List);
    }

    public static string GetFullRef(this MemberInfo member) {
      return $"{member.DeclaringType.Name}.{member.Name}";
    }

    public static ObjectTypeDef GetOperationDef(this GraphQLApiModel model, OperationType opType) {
      switch(opType) {
        case OperationType.Query: return model.QueryType;
        case OperationType.Mutation: return model.MutationType;
        default:
        case OperationType.Subscription: return model.SubscriptionType; 
      }
    }

    public static IList<T> GetTypeDefs<T>(this GraphQLApiModel model, TypeKind kind, 
                                           bool excludeHidden = false) where T : TypeDefBase {
      var temp = model.Types.Where(td => td.Kind == kind)
                            .Where(td => !excludeHidden || !td.Hidden)
                            .ToList();
      return temp.OfType<T>().ToList();
    }

    public static TypeDefBase GetTypeDef(this GraphQLApiModel model, Type type) {
      if(model.TypesByClrType.TryGetValue(type, out var typeDef))
        return typeDef;
      return null;
    }

    public static ScalarTypeDef GetScalarTypeDef(this GraphQLApiModel model, Type type) {
      return (ScalarTypeDef)model.GetTypeDef(type); 
    }

    public static ScalarTypeDef GetScalarTypeDef(this GraphQLApiModel model, string name) {
      if(model.TypesByName.TryGetValue(name, out var typeDef))
        return (ScalarTypeDef)typeDef;
      return null;
    }

    public static bool IsEnumFlagArray(this TypeDefBase typeDef) {
      return typeDef.Kind == TypeKind.Enum && (typeDef is EnumTypeDef etd && etd.IsFlagSet);
    }

    public static IList<string> GetRequiredFields(this InputObjectTypeDef inputTypeDef) {
      var reqFNames = inputTypeDef.Fields.Where(f => f.TypeRef.Kind == TypeKind.NonNull)
        .Select(f => f.Name).ToList();
      return reqFNames; 
    }

    public static bool IsSchemaType(this TypeDefBase typeDef) {
      switch(typeDef.TypeRole) {
        case ObjectTypeRole.ModuleMutation:
        case ObjectTypeRole.ModuleQuery:
        case ObjectTypeRole.ModuleSubscription:
          return false;
        default:
          return true; 
      }
    }

    public static void ForEachModelObject(this GraphQLApiModel model, Action<GraphQLModelObject> action) {
      foreach (var typeDef in model.Types) 
        if (typeDef.IsSchemaType()) // skip Module-Level transient types (ModuleQuery, ModuleMutation etc) 
          typeDef.ApplyDeep(action);
      foreach (var dirDef in model.Directives.Values)
        dirDef.ApplyDeep(action); 
    } //method

    public static void ApplyDeep(this GraphQLModelObject modelObj, Action<GraphQLModelObject> action) {
      action(modelObj);
      switch (modelObj) {
        case ComplexTypeDef ctd: // object type and interface type
          foreach (var fld in ctd.Fields)
            ApplyDeep(fld, action);
          break;

        case InputObjectTypeDef itd:
          foreach (var f in itd.Fields)
            ApplyDeep(f, action);
          break;

        case EnumTypeDef etd:
          foreach (var ev in etd.EnumValues)
            ApplyDeep(ev, action);
          break;

        case ScalarTypeDef _:
        case UnionTypeDef _:
        case InputValueDef _:
          // nothing to do
          break;

        case FieldDef fd:
          if (fd.Args != null)
            foreach (var a in fd.Args)
              ApplyDeep(a, action);
          break;

        case DirectiveDef dirDef:
          if (dirDef.Args != null)
            foreach (var a in dirDef.Args)
              ApplyDeep(a, action); 
          break; 
      } //switch
    }


  } //class
}
