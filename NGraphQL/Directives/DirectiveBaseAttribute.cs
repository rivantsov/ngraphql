using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  // base class for attibute-declared directives, ex: DeprecatedDir
  // derived attribute should be decoreated with [DirectiveInfo(..)] attribute
  public abstract class DirectiveBaseAttribute: Attribute {
    public readonly object[] ArgValues; 
    public DirectiveBaseAttribute(params object[] argValues) {
      ArgValues = argValues;
    }
  }

}
