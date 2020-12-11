using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  /// <summary> </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class DirectiveInfoAttribute : Attribute {
    public readonly DirectiveInfo Info;

    public DirectiveInfoAttribute(string name, DirectiveLocation locations, string description = null,
           bool listInSchema = true, bool isDeprecated = false, string deprecationReason = null) {
      Info = new DirectiveInfo() {
        Name = name,
        Locations = locations,
        Description = description,
        ListInSchema = listInSchema,
        IsDeprecated = isDeprecated,
        DeprecationReason = deprecationReason,
      };
    }
  }


}
