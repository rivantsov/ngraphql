using System;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst.Internals {

  public class DirectiveRegistration {
    public Type DirectiveType; 
    public string Name;
    public string Description;
    public DirectiveLocation Locations;
    public bool ListInSchema;
    // set to false if the directive has no runtime handler, it is purely declarative, like @deprecated
    public bool HasRuntimeHandler = true; 
  }

}
