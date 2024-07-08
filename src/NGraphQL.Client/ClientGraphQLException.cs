using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGraphQL.Client {

  public class ClientGraphQLException: GraphQLException {
    public readonly GraphQLError[] Errors;
    public readonly string ErrorsAsText;

    public ClientGraphQLException(IList<GraphQLError> errors, Exception inner = null): base("Server returned errors", inner) {
      Errors = errors.ToArray();
      ErrorsAsText = string.Join(Environment.NewLine, errors);
    }
  }
}
