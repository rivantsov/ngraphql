using System;
using System.Collections.Generic;
using System.Security.Claims;
using NGraphQ.Runtime;

namespace NGraphQL.CodeFirst {

  public interface IRequestContext {
    ClaimsPrincipal User { get; set; }
    void AddError(GraphQLError error, Exception sourceException = null);
    bool Failed { get; }

    /// <summary>Values for use by app code - resolvers etc. </summary>
    IDictionary<string, object> CustomData { get; }

    /// <summary>Protocol context; for HTTP server - HttpContext instance (ASP.NET Core). </summary>
    object GraphQLHttpRequest { get; }
  }

}
