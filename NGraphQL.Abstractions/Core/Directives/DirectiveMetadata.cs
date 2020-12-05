using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  public class DirectiveMetadata {
    public string Name;
    public string Description;
    public DirectiveLocation Locations;
    public bool ListInSchema;
    public bool IsDeprecated;
    public string DeprecationReason;
  }

}
