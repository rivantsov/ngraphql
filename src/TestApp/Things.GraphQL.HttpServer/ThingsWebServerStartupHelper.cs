using System.Threading.Tasks;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NGraphQL.Server;
using NGraphQL.Server.AspNetCore;

namespace Things.GraphQL.HttpServer {

  public static class ThingsWebServerStartupHelper {

    /// <summary>Starts GraphQL Web Server. </summary>
    /// <param name="args">Command line args.</param>
    /// <param name="useGraphiql">Set to true to launch Graphiql UI tool.</param>
    /// <param name="enablePreviewFeatures">Set to true to enable Query methods that use GraphQL preview features (using Input types as output field types).
    ///  Do not use this option with Graphiql, using input types as output crashes Graphiql's introspection query.   </param>
    /// <param name="serverUrl">Optional, use it when there is no launchSettings file; for ex: unit tests </param>
    /// <returns>A task running the server.</returns>
    public static Task StartThingsGraphqQLWebServer(string[] args, bool useGraphiql = false, bool enablePreviewFeatures = false, string serverUrl = null) {

      var builder = WebApplication.CreateBuilder(args);
      if (serverUrl != null) 
        builder.WebHost.UseUrls(serverUrl); //this is for unit tests only

      // create and register GraphQLHttpService
      var graphQLServer = CreateThingsHttpServer(enablePreviewFeatures);
      builder.Services.AddSingleton<GraphQLHttpHandler>(graphQLServer);
      // add controllers and add ref to assembly that contains our DefaultGraphQlController
      var graphqlControllerAssembly = typeof(DefaultGraphQLController).Assembly;
      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(graphqlControllerAssembly));



      var app = builder.Build();

      if (!app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseHsts();
      }

      app.UseRouting();

      app.MapControllerRoute(
        name: "default",
        pattern: "{controller=DefaultGraphQL}/{action}"
      );

      if (useGraphiql)
        app.UseGraphQLGraphiQL("/ui/graphiql", new GraphiQLOptions() { GraphQLEndPoint = "/graphql" });

      var task = Task.Run(() => app.Run());
      return task; 
    }

    private static GraphQLHttpHandler CreateThingsHttpServer(bool enablePreviewFeatures) {
      // create biz app, graphql httpGraphQLServer and Http graphQL httpGraphQLServer 
      var thingsBizApp = new ThingsApp();
      var serverStt = new GraphQLServerSettings() { Options = GraphQLServerOptions.DefaultDev };
      var thingsServer = new ThingsGraphQLServer(thingsBizApp, serverStt);
      if (!enablePreviewFeatures)
        thingsServer.DisablePreviewFeatures();
      thingsServer.Initialize(); 
      // finally httpServer
      var httpGraphQLServer = new GraphQLHttpHandler(thingsServer);
      return httpGraphQLServer;
    }



  }
}
