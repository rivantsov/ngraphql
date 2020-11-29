using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Runtime;

namespace NGraphQL.Core {

  public class ScalarException : GraphQLException {
    public readonly TokenData Token; 
    public ScalarException(string message, TokenData token) : base(message) {
      Token = token; 
    }
  }

  public static class ScalarExtensions {

    public static void ThrowScalarError(this IScalarContext requestContext, string message, TokenData token) {
      throw new ScalarException(message, token);
    }

  }
}
