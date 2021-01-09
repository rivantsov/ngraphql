using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  public static partial class ExecutionExtensions {

    public static bool IsSet(this GraphQLServerOptions options, GraphQLServerOptions option) {
      return (options & option) != 0;
    }

    public static void AbortIfFailed(this RequestContext context) {
      if (context.Failed)
        throw new AbortRequestException();
    }

    public static void AbortIfFailed(this FieldContext context) {
      if (context.RequestContext.Failed)
        throw new AbortRequestException();
    }

  }
}
