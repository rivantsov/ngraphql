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
    // Query directives only
    IRequestContext RequestContext { get; }
    Location SourceLocation { get; }
    DirectiveMetaDataAttribute Info { get; }
  }

  /// <summary>Base class for directives - classes implementing directives. </summary>
  public abstract class Directive {
    public readonly IDirectiveContext Context; 

    public Directive(IDirectiveContext context) {
      Context = context; 
    }

    public virtual object GetData(IRequestContext context) {
      return null; 
    }
  }

}
