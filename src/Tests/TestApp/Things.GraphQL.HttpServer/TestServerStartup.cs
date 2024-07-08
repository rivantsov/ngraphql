using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NGraphQL.Server;
using NGraphQL.Server.AspNetCore;
using NGraphQL.Server.Subscriptions;

namespace Things.GraphQL.HttpServer {

  // Static startup class to be used both by standalone Web App (this project) and unit test project to start 
  // local instance of GraphQL Http server. Normally this stuff would be directly in Program.cs
  public static class TestServerStartup {

    /// <summary>Starts GraphQL Web Server. </summary>
    /// <param name="args">Command line args.</param>
    /// <param name="enablePreviewFeatures">Set to true to enable Query methods that use GraphQL preview features (using Input types as output field types).
    ///  Do not use this option with Graphiql, using input types as output crashes Graphiql's introspection query.   </param>
    /// <param name="serverUrl">Optional, use it when there is no launchSettings file; for ex: unit tests </param>
    /// <returns>A task running the server.</returns>
    public static Task SetupServer(string[] args, bool enablePreviewFeatures = false, string serverUrl = null) {

      var builder = WebApplication.CreateBuilder(args);
      if (serverUrl != null) 
        builder.WebHost.UseUrls(serverUrl); //this is for unit tests only

      // create and register GraphQLHttpService
      var graphQLServer = CreateTestGraphQLServer(enablePreviewFeatures);
      builder.AddGraphQLServer(graphQLServer); 

      var app = builder.Build();
      app.UseRouting();
      // maps GraphQL endpoint to '/graphql', subscriptions to '/graphql/subscriptions'
      app.MapGraphQLEndpoint(graphQLServer.Settings);

      var task = Task.Run(() => app.Run());
      return task; 
    }

    private static GraphQLServer CreateTestGraphQLServer(bool enablePreviewFeatures) {
      // create biz app, graphql server 
      var thingsBizApp = new ThingsApp();
      // Note: By default now server ignores when a resolver for non-null field returns null.
      // In another test project we explicitly turn it off, to make server detect it and throw error
      // - we have a special test for this. In this test project we are OK with default behavior
      var serverStt = new GraphQLServerSettings() { Options = GraphQLServerOptions.DefaultDev };
      var thingsServer = new ThingsGraphQLServer(thingsBizApp, serverStt);
      if (!enablePreviewFeatures)
        thingsServer.DisablePreviewFeatures();
      thingsServer.Initialize();
      return thingsServer; 
    }



  }
}
