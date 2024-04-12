using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace NGraphQL.Server.AspNetCore {
  public static class GraphQLStartupExtensions {
    
    public static WebApplicationBuilder AddGraphQLServer(this WebApplicationBuilder builder, GraphQLServer graphQLServer) {
      var graphQLHttpHandler = new GraphQLHttpHandler(graphQLServer);
      builder.Services.AddSingleton<GraphQLHttpHandler>(graphQLHttpHandler);
      // add controllers and add ref to assembly that contains our DefaultGraphQlController (this assembly)
      var graphqlControllerAssembly = typeof(DefaultGraphQLController).Assembly;
      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(graphqlControllerAssembly));
      return builder; 
    }

    public static WebApplication MapGraphQLEndpoint(this WebApplication app) {
      app.MapControllerRoute(
        name: "default",
        pattern: "{controller=DefaultGraphQL}/{action}"
      ); 
      return app; 
    }
  
  }
}
