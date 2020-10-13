using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using NGraphQL.Http;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using StarWars.Api;

namespace StarWars.HttpServer {
  class Program {
    public static string ServiceUrl = "http://127.0.0.1:60000";
    public static string LogFilePath = "_serverLog.log";
    public static string SchemaFilePath = "_starWarsSchema.txt";
    public static GraphQLHttpServer StarWarsHttpServer;
    static IWebHost _webHost;
    public static string SampleUrl = ServiceUrl + "?query={starships{name,length}}";


    static void Main(string[] args) {
      try {
        Console.WriteLine("StarWars HttpServer starting...");
        Initialize();
        Console.WriteLine("Server started at " + ServiceUrl);
        // printout schema doc
        var schemaDoc = StarWarsHttpServer.Server.Api.Model.SchemaDoc;
        File.WriteAllText(SchemaFilePath, schemaDoc);
        Console.WriteLine($"  StarWars Schema document saved in {SchemaFilePath}");
        Console.WriteLine($"  Sample URL to try in browser:  {SampleUrl}");
        Console.WriteLine($"  ... or open graphicl.html page in Graphicl folder to play with the API.");
      } catch (ServerStartupException ex) {
        Console.WriteLine("Startup failed, errors: " + Environment.NewLine + ex.GetErrorsAsText());
      } catch (Exception ex) {
        Console.WriteLine(ex.ToString());
      } finally {
        Console.Write("Press any key to exit...");
        Console.WriteLine();
        Console.ReadKey();
        ShutDown();
      }
    }

    public static void Initialize() {
      if (StarWarsHttpServer != null) //already initialized
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);

      // create server and Http graphQL server 
      var app = new StarWarsApp();
      var starWarsApi = new StarWarsApi(app);
      var starWarsServer = new GraphQLServer(starWarsApi);
      starWarsServer.Initialize();
      StarWarsHttpServer = new GraphQLHttpServer(starWarsServer);
      StartWebHost();
    }

    private static void StartWebHost() {
      var hostBuilder = WebHost.CreateDefaultBuilder()
          .ConfigureAppConfiguration((context, config) => { })
          .UseStartup<Startup>()
          .UseUrls(ServiceUrl)
          ;
      _webHost = hostBuilder.Build();
      Task.Run(() => _webHost.Run());
      Thread.Sleep(1000);
      Debug.WriteLine("The service is running on URL: " + ServiceUrl);
    }

    public static void ShutDown() {
      _webHost?.StopAsync().Wait();
    }

  }
}
