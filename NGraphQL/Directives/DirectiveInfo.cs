using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  public class DirectiveInfo {
    public string Name;
    public string Description;
    public DirectiveLocation Locations;
    public bool ListInSchema;
    public bool IsDeprecated;
    public string DeprecationReason;
    // set to false if the directive has no runtime handler, it is purely declarative, like @deprecated
    public bool HasRuntimeHandler = true; 
  }

}
