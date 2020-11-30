using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Introspection;

namespace NGraphQL.Core {
  
  public interface IDirectiveInfo {
    string Name { get; }
    string Description { get; }
    DirectiveLocation Locations { get; }
    bool ListInSchema { get; }
    bool IsDeprecated { get; }
    string DeprecationReason { get; }

  }

}
