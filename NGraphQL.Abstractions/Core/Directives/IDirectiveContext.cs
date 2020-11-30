using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Runtime;

namespace NGraphQL.Core {

  /// <summary>Provides a context information for directive instance. </summary>
  public interface IDirectiveContext {
    DirectiveLocation Locaton { get; }
    IDirectiveInfo MetaData { get; }
    // Query directives only
    Location SourceLocation { get; }
  }

}
