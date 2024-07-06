using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Subscriptions;

namespace NGraphQL.Server.AspNetCore {

  public static class GraphQLStartupExtensions {
    
    public static WebApplicationBuilder AddGraphQLServer(this WebApplicationBuilder builder, GraphQLServer graphQLServer) {
      builder.Services.AddSingleton<GraphQLServer>(graphQLServer);
      var graphQLHttpHandler = new GraphQLHttpHandler(graphQLServer);
      builder.Services.AddSingleton<GraphQLHttpHandler>(graphQLHttpHandler);

      if (graphQLServer.Settings.Features.IsSet(GraphQLServerFeatures.Subscriptions)) {
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IMessageSender, SignalRSender>();
      }

      // add controllers and add ref to assembly that contains our DefaultGraphQlController (this assembly)
      var graphqlControllerAssembly = typeof(DefaultGraphQLController).Assembly;
      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(graphqlControllerAssembly));
      return builder; 
    }

    public static WebApplication MapGraphQLEndpoint(this WebApplication app, GraphQLServerSettings settings) {
      app.MapControllerRoute(
        name: "default",
        pattern: "{controller=DefaultGraphQL}/{action}"
      ); 
      if (settings.Features.IsSet(GraphQLServerFeatures.Subscriptions)) {
        app.MapHub<SignalRListener>(settings.SubscriptionsEndpoint);
      }
      return app; 
    }
  
  }
}

