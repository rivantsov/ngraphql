using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  /// <summary> </summary>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
  public class DefineDirectiveAttribute : Attribute, IDirectiveInfo {
    public string Name { get; }
    public string Description { get; }
    public DirectiveLocation Locations { get; }
    public bool ListInSchema { get; }
    public bool IsDeprecated { get; set; }
    public string DeprecationReason { get; }

    public object[] ArgValuess; 

    public DefineDirectiveAttribute(string name, DirectiveLocation locations, string description = null,
           bool listInSchema = true, bool isDeprecated = false, string deprecationReason = null) {
      Name = name;
      Locations = locations;
      Description = description;
      ListInSchema = listInSchema;
      IsDeprecated = isDeprecated;
      DeprecationReason = deprecationReason;
    }
  }

  public interface IDirectiveInfo {
    string Name { get; }
    string Description { get; }
    DirectiveLocation Locations { get; }
    bool ListInSchema { get; }
    bool IsDeprecated { get; }
    string DeprecationReason { get; }
  }

}
