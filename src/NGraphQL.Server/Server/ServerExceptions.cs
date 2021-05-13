using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Request;

namespace NGraphQL.Server {

  public class ServerStartupException : Exception {
    public IList<string> Errors; 
    public ServerStartupException(IList<string> errors) 
           : base("GraphQL Server startup failed. See details in  Errors property of this exception, " + 
               " or in server.StartupErrors property or in Trace output.") {
      Errors = errors; 
    }

    public string GetErrorsAsText() {
      var errText = string.Join(Environment.NewLine, this.Errors);
      return errText; 
    }
  }

  public class ResolverException : GraphQLException {
    public ResolverException(Exception ex) : base("Resolver exception", ex) { }
    public ResolverException(string message) : base(message) { }
  }

  public class InvalidInputException : GraphQLException {
    public RequestObjectBase Anchor;
    public InvalidInputException(string message, RequestObjectBase anchor, Exception inner = null)
             : base(message, inner) {
      Anchor = anchor;
    }
    public InvalidInputException(NamedRequestObject anchor, Exception inner) : base(inner.Message, inner) {
      Anchor = anchor;
    }
  }

  public class FatalServerException : GraphQLException {
    public FatalServerException(string message, Exception ex = null) : base(message, null) { }
  }



}
