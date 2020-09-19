using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;
using NGraphQL.Model.Request;

namespace NGraphQL.Server {

  public class GraphQLResponse {
    public IList<GraphQLError> Errors = new List<GraphQLError>();
    public IDictionary<string, object> Data;
  }
}
