using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQ.Runtime {

  public class GraphQLRequest {
    public string OperationName;
    public string Query;
    public IDictionary<string, object> Variables = new Dictionary<string, object>();

  }
}
