using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace NGraphQL.CodeFirst {

  public interface IRequestContext {
    object App { get; }
    ClaimsPrincipal User { get; set; }
    
    void AddError(GraphQLError error, Exception sourceException = null);
    bool Failed { get; }
    
    /// <summary>Values for use by app code - resolvers etc. </summary>
    IDictionary<string, object> CustomData { get; }

    /// <summary>Services from the host - AspNetCore DI service container. </summary>
    IServiceProvider HostServices { get; }

    /// <summary>Protocol context; for HTTP server - HttpContext instance (ASP.NET Core). </summary>
    object HttpContext { get; }

    /// <summary>Reserved for VITA ORM operation context. </summary>
    object VitaOperationContext { get; }
  }

}
