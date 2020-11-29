using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public static partial class ModelExtensions {

    /*
    public static bool IsInterfaceBox(this Type type, out Type intType) {
      intType = null; 
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InterfaceBox<>)) {
        intType = type.GetGenericArguments()[0];
        return true; 
      }
      return false; 
    }
    */
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

    public static Directive CreateDirective(this DirectiveDef def, params object[] argValues) {
      var argVcount = argValues == null ? 0 : argValues.Length; 
      if(def.Args.Count != argVcount) 
        throw new Exception($"Arg count mismatch for directive {def.Name}. ");
      var dir = new Directive() { Def = def, Name = def.Name, ArgValues = argValues };
      return dir; 
    }

    public static bool IsEnumFlagArray(this TypeDefBase typeDef) {
      return typeDef.Kind == TypeKind.Enum && (typeDef is EnumTypeDef etd && etd.IsFlagSet);
    }

    public static IList<string> GetRequiredFields(this InputObjectTypeDef inputTypeDef) {
      var reqFNames = inputTypeDef.Fields.Where(f => f.TypeRef.Kind == TypeKind.NotNull)
        .Select(f => f.Name).ToList();
      return reqFNames; 
    }

  } //class
}
