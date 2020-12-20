using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  public enum AdjustmentType {
    IgnoreMember,
    GraphQLName,
  }

  public class AddedAttributeInfo {
    public Type Type; 
    public string MemberName;
    public string ArgName;
    public Attribute Attribute; 
  }
}
