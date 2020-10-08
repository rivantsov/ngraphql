using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NGraphQL.Http;

namespace StarWars.HttpServer {

  public class Startup {

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddCors(options =>
      {
        options.AddPolicy(name: "AllowAll", builder => { 
          builder.WithOrigins("*"); builder.WithHeaders("*");
        });
      });
      services.AddSingleton(typeof(GraphQLHttpServer), Program.StarWarsHttpServer);
      // clear out default configuration - it installs Console output logger which sends out tons of garbage
      //  https://weblog.west-wind.com/posts/2018/Dec/31/Dont-let-ASPNET-Core-Default-Console-Logging-Slow-your-App-down
      services.AddLogging(config => {
        config.ClearProviders();
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app) {
      app.UseCors("AllowAll");
      // that sets the GraphQL Http server to handle all requests
      app.UseGraphQLServer(Program.StarWarsHttpServer);
      // if you want to add authentication, you can add middleware as well; you should set httpContext.User (ClaimsPrincipal)
      //  to set of claims of authenticated user (like 'userId' = 123); this value will be passed to resolver methods level
      //  (in resolver method: fieldContext.RequestContext.User)
    }
  }
}
