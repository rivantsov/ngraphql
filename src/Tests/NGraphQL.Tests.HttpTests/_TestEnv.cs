using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

using NGraphQL.Client;
using NGraphQL.Utilities;

namespace NGraphQL.Tests.HttpTests {
  public static class TestEnv {
    public static string ServiceUrl = "http://127.0.0.1:55000";
    public static string GraphQLEndPointUrl = ServiceUrl + "/graphql";
    public static GraphQLClient Client;
    static IWebHost _webHost;
    public static string LogFilePath = "_graphQLHttpTests.log";

    public static void Initialize() {
      if (Client != null) //already initialized
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);

      StartWebHost();
      Client = new GraphQLClient(GraphQLEndPointUrl);
      Client.RequestCompleted += Client_RequestCompleted;
    }

    private static void StartWebHost() {
      var hostBuilder = WebHost.CreateDefaultBuilder()
          .ConfigureAppConfiguration((context, config) => { })
          .UseStartup<TestHttpServer.TestServerStartup>()
          .UseUrls(ServiceUrl)
          ;
      _webHost = hostBuilder.Build();
      Task.Run(() => _webHost.Run());
      Debug.WriteLine("The service is running on URL: " + ServiceUrl);
    }

    public static void ShutDown() {
      _webHost?.StopAsync().Wait();
    }

    public static void LogTestMethodStart([CallerMemberName] string testName = null) {
      LogText($@"

==================================== Test Method {testName} ================================================
");
    }

    public static void LogTestDescr(string descr) {
      LogText($@"
Testing: {descr}
");
    }

    private static void Client_RequestCompleted(object sender, RequestCompletedEventArgs e) {
      LogCompletedRequest(e.Response);
    }

    public static void LogCompletedRequest(ServerResponse response) {
      string reqText;
      var req = response.Request; 
      if (req.HttpMethod == "GET") {
        reqText = @$"GET, URL: {req.UrlQueryPartForGet} 
                unescaped: {Uri.UnescapeDataString(req.UrlQueryPartForGet)}";
      } else {
        // for better readability, unescape \r\n; Json serializer escapes new-line symbols inside strings,
        var bodyUnesc = req.Body.Replace("\\r\\n", Environment.NewLine);
        reqText = "POST, payload: " + Environment.NewLine + bodyUnesc;
      }
      var jsonResponse = JsonConvert.SerializeObject(response.TopFields, Formatting.Indented);
      var text = $@"
Request: 
{reqText}

Response:
{jsonResponse}

//  time: {response.DurationMs} ms
----------------------------------------------------------------------------------------------------------------------------------- 

";
      LogText(text);
      if (response.Exception != null)
        LogText(response.Exception.ToText());
    }


    static object _lock = new object();
    public static void LogText(string text) {
      lock (_lock) {
        File.AppendAllText(LogFilePath, text);
      }
    }

  }
}
