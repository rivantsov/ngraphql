using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQ.Runtime {

  public class GraphQLResponse {
    public IList<GraphQLError> Errors = new List<GraphQLError>();
    public IDictionary<string, object> Data;
  }
}
