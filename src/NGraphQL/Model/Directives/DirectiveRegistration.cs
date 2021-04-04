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
    public IDirectiveHandler Handler; 
  }

}
