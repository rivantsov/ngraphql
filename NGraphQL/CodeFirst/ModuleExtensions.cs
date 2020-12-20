using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {
  
  public static class ModuleExtensions {

    public static void HideMember(this GraphQLModule module, Type type, string memberName) {
      module.Adjustments.Add(new AddedAttributeInfo() { 
        Type = type, MemberName = memberName, Attribute = new HiddenAttribute() 
      });
    }
    public static void IgnoreMember(this GraphQLModule module, Type type, string memberName) {
      module.Adjustments.Add(new AddedAttributeInfo() {
        Type = type, MemberName = memberName, Attribute = new IgnoreAttribute()
      });
    }

    public static void SetTypeName(this GraphQLModule module, Type type, string name) {
      module.Adjustments.Add(new AddedAttributeInfo() {
        Type = type, Attribute = new GraphQLNameAttribute(name)
      });
    }

    public static void SetMemberName(this GraphQLModule module, Type type, string memberName, string name) {
      module.Adjustments.Add(new AddedAttributeInfo() {
        Type = type, MemberName = memberName, Attribute = new GraphQLNameAttribute(name)
      });
    }


  }
}
