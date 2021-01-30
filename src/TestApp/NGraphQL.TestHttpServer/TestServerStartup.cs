using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NGraphQL.Server;
using NGraphQL.Server.AspNetCore;
using NGraphQL.TestApp;

namespace NGraphQL.TestHttpServer {

  public class TestServerStartup {

    GraphQLHttpServer _graphQLServer;

    public TestServerStartup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }
      //app.UseHttpsRedirection();
      app.UseRouting();

      // create server and configure GraphQL endpoints
      _graphQLServer = CreateGraphQLHttpServer();
      app.UseEndpoints(endpoints => {
        endpoints.MapPost("graphql", HandleRequest);
        endpoints.MapGet("graphql", HandleRequest);
        endpoints.MapGet("graphql/schema", HandleRequest);
      });
      // Use GraphiQL UI
      app.UseGraphiQLServer();
    }

    private Task HandleRequest(HttpContext context) {
      return _graphQLServer.HandleGraphQLHttpRequestAsync(context);
    }

    private GraphQLHttpServer CreateGraphQLHttpServer() {
      // create server and Http graphQL server 
      var thingsBizApp = new ThingsApp();
      var thingsServer = new GraphQLServer(thingsBizApp);
      var thingsModule = new ThingsGraphQLModule();
      thingsServer.RegisterModules(thingsModule);
      var server = new GraphQLHttpServer(thingsServer);
      return server;
    }

  }
}
