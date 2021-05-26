using System;
using System.Reflection;

using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public class DirectiveRegistration {
    public string Name;
    public string Description;
    public DirectiveLocation Locations;
    public Type AttributeType; // for type system directives defined by attributes
    public MethodBase Signature; // for query directives, defined by method signature
    public bool ListInSchema;
  }

  // Directive handler (implementation) is defined separately from directive itself.
  //  this allows handlers to use/reference heavy server-side logic/types, while directive itself 
  //  can be light and used in shared model definitions. 
  public class DirectiveHandlerInfo {
    public string Name;
    public Type Type; 
  }

}
