using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace NGraphQL.Http {

  public static class GraphQLHttpServerExtensions {

    // For Authentication/authorization - use appropriate middleware and set httpContext.User (ClaimsPrincipal); 
    //  this value will be passed to RequestContext and can be retrieved by resolver methods. 
    // Claims principal can be used just as a dict of key-value pairs, like 'userId'=123
    public static void UseGraphQLServer(this IApplicationBuilder app, GraphQLHttpServer server) {
      app.Run(httpContext => server.HandleGraphQLHttpRequestAsync(httpContext));
    }

  }

}
