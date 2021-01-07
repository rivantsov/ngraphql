using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
      services.AddControllers().AddNewtonsoftJson().AddApplicationPart(typeof(GraphQLController).Assembly);
      
      // create server and Http graphQL server 
      var thingsBizApp = new ThingsApp();
      var thingsServer = new GraphQLServer(thingsBizApp);
      var thingsModule = new ThingsGraphQLModule();
      thingsServer.RegisterModules(thingsModule);
      thingsServer.Initialize();
      var httpServer = new GraphQLHttpServer(thingsServer);
      services.AddSingleton(httpServer);

      /*
      var varDeserializer = new JsonVariablesDeserializer();
      thingsServer.Events.RequestPrepared += (sender, e) => {
        if (e.RequestContext.Operation.Variables.Count == 0)
          return;
        varDeserializer.PrepareRequestVariables(e.RequestContext);
      };
      */

      services.AddSingleton(thingsServer);
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
        endpoints.MapControllers();
      });
      
      // Use GraphiQL UI
      app.UseGraphiQLServer();
    }

  }
}
