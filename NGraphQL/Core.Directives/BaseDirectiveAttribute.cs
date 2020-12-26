using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Core {

  public abstract class BaseDirectiveAttribute: Attribute {
    public object[] ArgValues { get; }
    public BaseDirectiveAttribute(params object[] argValues) {
      ArgValues = argValues; 
    }
  }
}
