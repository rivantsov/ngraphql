using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server {

  public class GraphQLRequest {
    public string OperationName;
    public string Query;
    public IDictionary<string, object> Variables = new Dictionary<string, object>();

  }
}
