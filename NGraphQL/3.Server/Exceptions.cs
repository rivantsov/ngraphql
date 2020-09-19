using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Server.Execution {

  public class GraphQLException : Exception {
    public GraphQLException(string message, Exception inner = null) : base(message, inner) { }
  }

  public class AbortRequestException: GraphQLException {
    public AbortRequestException() : base("aborted") { }
  }

  public class InvalidInputException: GraphQLException {
    public RequestObjectBase Anchor; 
    public InvalidInputException(string message, RequestObjectBase anchor, Exception inner = null)
             : base(message, inner) {
      Anchor = anchor; 
    }
    public InvalidInputException(NamedRequestObject anchor, Exception inner): base(inner.Message, inner) {
      Anchor = anchor;
    }
  }

  public class ServerStartupException : Exception {
    public IList<string> Errors; 
    public ServerStartupException(IList<string> errors) 
           : base("GraphQL Server startup failed. See details in  Errors property of this exception, " + 
               " or in server.StartupErrors property or in Trace output.") {
      Errors = errors; 
    }
  }

  public class ResolverException : GraphQLException {
    public ResolverException(Exception ex) : base("Resolver exception", ex) { }
    public ResolverException(string message) : base(message) { }
  }


}
