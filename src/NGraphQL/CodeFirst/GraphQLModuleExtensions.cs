using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst.Internals;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst {
  
  public static class GraphQLModuleExtensions {

    public static void HideMember(this GraphQLModule module, Type type, string memberName) {
      module.Adjustments.Add(new ModelAdjustment() { 
        Type = type, MemberName = memberName, Attribute = new HiddenAttribute() 
      });
    }

    public static void IgnoreMember(this GraphQLModule module, Type type, string memberName) {
      module.Adjustments.Add(new ModelAdjustment() {
        Type = type, MemberName = memberName, Attribute = new IgnoreAttribute()
      });
    }

    public static void SetTypeName(this GraphQLModule module, Type type, string name) {
      module.Adjustments.Add(new ModelAdjustment() {
        Type = type, Attribute = new GraphQLNameAttribute(name)
      });
    }

    public static void SetMemberName(this GraphQLModule module, Type type, string memberName, string name) {
      module.Adjustments.Add(new ModelAdjustment() {
        Type = type, MemberName = memberName, Attribute = new GraphQLNameAttribute(name)
      });
    }

    // To allow add multiple types to module's types lists
    public static void Add(this IList<Type> list, params Type[] types) {
      foreach (var type in types)
        list.Add(type);
    }

  }
}
