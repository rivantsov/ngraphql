using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NGraphQL.Tests.HttpTests {

  public class TestStartup {

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddSingleton(TestEnv.ThingsHttpServer);
      // clear out default configuration - it installs Console output logger which sends out tons of garbage
      //  https://weblog.west-wind.com/posts/2018/Dec/31/Dont-let-ASPNET-Core-Default-Console-Logging-Slow-your-App-down
      services.AddLogging(config => {
        config.ClearProviders();
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app) {

      app.UseRouting();

      var server = TestEnv.ThingsHttpServer;
      app.UseEndpoints(endpoints => {
        endpoints.MapPost("graphql", async context => await server.HandleGraphQLHttpRequestAsync(context));
        endpoints.MapGet("graphql", async context => await server.HandleGraphQLHttpRequestAsync(context));
        endpoints.MapGet("graphql/schema", async context => await server.HandleGraphQLHttpRequestAsync(context));
      });
    }

  }
}
