using System;
using System.Collections.Generic;
using System.Text;
using NGraphQ.Runtime;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Introspection;

namespace NGraphQL.Core.Directives {

  /// <summary>Provides a context information for directive instance. </summary>
  public interface IDirectiveContext {
    __DirectiveLocation Locaton { get; }
    // Query directives only
    IRequestContext RequestContext { get; }
    Location SourceLocation { get; }
    DirectiveMetadataAttribute Info { get; }
  }

  /// <summary>Base class for directives - classes implementing directives. </summary>
  public abstract class Directive {
    public readonly IDirectiveContext Context; 

    public Directive(IDirectiveContext context) {
      Context = context; 
    }
  }

}
