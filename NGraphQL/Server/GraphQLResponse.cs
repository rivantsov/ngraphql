using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server {

  public class GraphQLResponse {
    public IList<GraphQLError> Errors = new List<GraphQLError>();
    public IDictionary<string, object> Data;
  }
}
