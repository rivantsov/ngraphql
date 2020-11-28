using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Introspection;

namespace NGraphQL.Core.Directives {

  public class DirectiveMetaDataAttribute : Attribute {
    public string Name;
    public string Description;
    public __DirectiveLocation Locations;
    public bool ListInSchema;
    public bool IsDeprecated;
    public string DeprecationReason;

    public DirectiveMetaDataAttribute(string name, __DirectiveLocation locations, string description = null,
           bool listInSchema = true) {
      Name = name;
      Locations = locations;
      Description = description;
      ListInSchema = listInSchema; 
    }
  }

}
