using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  public abstract class BaseDirectiveAttribute: Attribute {
    public object[] ArgValues { get; }
    public BaseDirectiveAttribute(params object[] argValues) {
      ArgValues = argValues; 
    }
  }
}
