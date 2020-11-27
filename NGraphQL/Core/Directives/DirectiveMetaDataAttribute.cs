using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Introspection;

namespace NGraphQL.Core.Directives {

  public class DirectiveMetadataAttribute : Attribute {
    public string Name;
    public string Description;
    public __DirectiveLocation Locations;
    public bool IsCustom;

    public DirectiveMetadataAttribute(string name, __DirectiveLocation locations, string description = null, bool isCustom = true) {
      Name = name;
      Locations = locations;
      Description = description;
      IsCustom = isCustom; 
    }
  }

}
