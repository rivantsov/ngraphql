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

    public static bool TypeIsTransient(this TypeRole typeRole) {
      switch(typeRole) {
        case TypeRole.ModuleQuery:
        case TypeRole.ModuleMutation:
        case TypeRole.ModuleSubscription:
          return true;
        default:
          return false; 
      }
    }

    public static IList<T> GetTypeDefs<T>(this GraphQLApiModel model, TypeKind kind, 
                                           bool excludeHidden = false) where T : TypeDefBase {
      var temp = model.Types.Where(td => td.Kind == kind)
                            .Where(td => !excludeHidden || !td.Hidden)
                            .ToList();
      return temp.OfType<T>().ToList();
    }

    public static ScalarTypeDef GetScalarTypeDef(this GraphQLApiModel model, Type type) {
      return (ScalarTypeDef)model.GetTypeDef(type); 
    }

    public static TypeDefBase LookupTypeDef(this GraphQLApiModel model, string name) {
      if(model.TypesByName.TryGetValue(name, out var typeDef))
        return typeDef;
      return null;
    }


    public static bool IsEnumFlagArray(this TypeDefBase typeDef) {
      return typeDef.Kind == TypeKind.Enum && (typeDef is EnumTypeDef etd && etd.Handler.IsFlagSet);
    }

    public static IList<string> GetRequiredFields(this InputObjectTypeDef inputTypeDef) {
      var reqFNames = inputTypeDef.Fields.Where(f => f.TypeRef.Kind == TypeKind.NonNull)
        .Select(f => f.Name).ToList();
      return reqFNames; 
    }

    public static bool IsDataType(this TypeDefBase typeDef) {
      if (typeDef is ComplexTypeDef cpxTypeDef)
        return cpxTypeDef.TypeRole == TypeRole.Data;
      return true; // non-object types: Scalars, enums, input types
    }

    public static bool IsModuleTransientType(this TypeDefBase typeDef) {
      var cpxTypeDef = typeDef as ComplexTypeDef;
      if (cpxTypeDef == null)
        return false; 
      switch(cpxTypeDef.TypeRole) {
        case TypeRole.ModuleMutation: case TypeRole.ModuleQuery: case TypeRole.ModuleSubscription: return true;
        default: return false;
      }
    }


    public static bool HasDirectives(this GraphQLModelObject modelObject) {
      return modelObject.Directives != null && modelObject.Directives.Count > 0; 
    }


    public static FieldResolverInfo GetResolver(this ObjectTypeMapping mapping, FieldDef fieldDef) {
      var res = mapping.FieldResolvers.FirstOrDefault(r => r.Field == fieldDef);
      return res; 
    }
  } //class
}
