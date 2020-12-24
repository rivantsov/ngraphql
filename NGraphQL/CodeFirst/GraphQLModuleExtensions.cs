using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst.Internals;
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

    public static void RegisterDirective(this GraphQLModule module, string name, Type directiveType, 
           DirectiveLocation locations, string description = null, bool listInSchema = true) {
      var reg = new DirectiveRegistration() {
        Name = name, DirectiveType = directiveType, Locations = locations, Description = description,
        ListInSchema = listInSchema,
      };
      module.RegisteredDirectives.Add(reg);
    }


  }
}
