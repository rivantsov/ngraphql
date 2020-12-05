using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  // base class for attibute-declared directives, ex: DeprecatedDir
  // derived attribute should be decoreated with [AttributteInfo(..)] attribute
  public abstract class DeclareDirectiveAttribute: Attribute {
    public readonly object[] ArgValues; 
    public DeclareDirectiveAttribute(params object[] argValues) {
      ArgValues = argValues;
    }
  }

}
