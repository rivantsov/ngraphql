using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Runtime;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  public class ScalarException : GraphQLException {
    public readonly TokenData Token; 
    public ScalarException(string message, TokenData token) : base(message) {
      Token = token; 
    }
  }

  public static class ScalarExtensions {

    public static void ThrowScalarError(this RequestContext requestContext, string message, TokenData token) {
      throw new ScalarException(message, token);
    }

  }
}
