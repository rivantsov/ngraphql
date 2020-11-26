using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.CodeFirst {

  public interface IRequestContext {
    ClaimsPrincipal User { get; set; }
    GraphQLOperation Operation { get; }
    IList<VariableValue> OperationVariables { get; }
    GraphQLResponse Response { get; }
    RequestMetrics Metrics { get; }
    void AddError(GraphQLError error, Exception sourceException = null);
    bool Failed { get; }

    /// <summary>Values for use by custom app code, resolvers etc. </summary>
    IDictionary<string, object> Values { get; }

    /// <summary>Protocol context; for HTTP server - HttpContext instance (ASP.NET Core). </summary>
    object GraphQLHttpRequest { get; }
    GraphQLServer Server { get; }
  }

}
