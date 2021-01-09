using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL {

  public class GraphQLException : Exception {
    public GraphQLException(string message, Exception inner = null) : base(message, inner) { }
  }

  public class AbortRequestException : GraphQLException {
    public AbortRequestException() : base("aborted") { }
  }


}
