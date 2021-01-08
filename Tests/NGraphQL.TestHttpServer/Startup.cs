using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NGraphQL.Server;
using NGraphQL.Server.Http;
using NGraphQL.TestApp;

namespace NGraphQL.TestHttpServer {

  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      var server = InitializeGraphQLServer();
      services.AddSingleton(server);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

     
      app.UseEndpoints(endpoints =>
      {
        //endpoints.MapControllers();
        endpoints.MapPost("graphql", async context => await HandleGraphQLRequestAsync(context) );
        endpoints.MapGet("graphql", async context => await HandleGraphQLRequestAsync(context));
        endpoints.MapGet("graphql/schema", async context => await HandleGraphQLRequestAsync(context));
      });
      
      // Use GraphiQL UI
      app.UseGraphiQLServer();
    }

    private GraphQLHttpServer InitializeGraphQLServer() {
      // create server and Http graphQL server 
      var thingsBizApp = new ThingsApp();
      var thingsServer = new GraphQLServer(thingsBizApp);
      var thingsModule = new ThingsGraphQLModule();
      thingsServer.RegisterModules(thingsModule);
      thingsServer.Initialize();
      _graphQlHttpServer = new GraphQLHttpServer(thingsServer);
      return _graphQlHttpServer; 
    }
    static GraphQLHttpServer _graphQlHttpServer;
    private static Task HandleGraphQLRequestAsync(HttpContext context) {
      return _graphQlHttpServer.HandleGraphQLHttpRequestAsync(context);
    }

  }
}
