using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL {

  public class GraphQLRequest {
    public string OperationName;
    public string Query;
    public IDictionary<string, object> Variables = new Dictionary<string, object>();

  }
}
