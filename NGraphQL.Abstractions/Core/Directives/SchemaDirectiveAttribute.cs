using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Core {

  public abstract class SchemaDirectiveAttribute: Attribute {
    public readonly object[] ArgValues; 
    public SchemaDirectiveAttribute(params object[] argValues) {
      ArgValues = argValues;
    }
  }

}
