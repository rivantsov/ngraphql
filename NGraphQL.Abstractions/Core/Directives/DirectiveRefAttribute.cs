using System;
using NGraphQL.Introspection;

namespace NGraphQL.Core {


  /// <summary>Base class for custom attributes referencing directive type.  </summary>
  public abstract class DirectiveRefAttribute: Attribute {
    public readonly Type DirectiveType; 

    public DirectiveRefAttribute(Type directiveType) {
      DirectiveType = directiveType; 
    }
  }
}
