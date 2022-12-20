using System;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NGraphQL.Server;
using NGraphQL.Server.AspNetCore;

namespace Things.GraphQL.HttpServer {

  public class Program
  {
    public static void Main(string[] args)
    {
      StartWebApp(args); 

      // CreateHostBuilder(args).Build().Run();
    }

    private static void StartWebApp(string[] args) {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddControllersWithViews();
      var graphQLServer = CreateTestGraphQLServer();
      builder.Services.AddSingleton<GraphQLHttpServer>(graphQLServer);

      var app = builder.Build();

      if (!app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseHsts();
      }

      //app.UseHttpsRedirection();
      //app.UseStaticFiles();

      app.UseRouting();

      //app.UseAuthorization();

      app.MapControllerRoute(
        name: "default",
        pattern: "{controller=DefaultGraphQL}/{action}"//=HandleRequest}"
      );

      // Use GraphiQL UI
      app.UseGraphQLGraphiQL("/ui/graphiql", new GraphiQLOptions() { GraphQLEndPoint = "/graphql" });

      app.Run();
    }

    private static GraphQLHttpServer CreateTestGraphQLServer() {
      // create server and Http graphQL server 
      var thingsBizApp = new ThingsApp();
      var serverStt = new GraphQLServerSettings() { Options = GraphQLServerOptions.DefaultDev };
      var thingsServer = new ThingsGraphQLServer(thingsBizApp, serverStt);
      var server = new GraphQLHttpServer(thingsServer);
      return server;
    }




    // Old code
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
              webBuilder.UseStartup<TestServerStartup>();
            });


  }
}
