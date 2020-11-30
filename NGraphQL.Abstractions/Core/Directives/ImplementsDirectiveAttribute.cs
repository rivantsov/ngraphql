using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [AttributeUsage(AttributeTargets.Method)]
  public class ImplementsDirectiveAttribute : Attribute, IDirectiveInfo {
    public string Name { get; }
    public string Description { get; }
    public DirectiveLocation Locations { get; }
    public bool ListInSchema { get; }
    public bool IsDeprecated { get; set; }
    public string DeprecationReason { get; set; }

    public ImplementsDirectiveAttribute(string name, DirectiveLocation locations, string description = null,
           bool listInSchema = true) {
      Name = name;
      Locations = locations;
      Description = description;
      ListInSchema = listInSchema; 
    }
  }

}
