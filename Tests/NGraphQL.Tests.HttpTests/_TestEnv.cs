using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using NGraphQL.Http;
using NGraphQL.Server;
using NGraphQL.TestApp;
using NGraphQL.Utilities;
using Arrest;
using NGraphQL.Client;

namespace NGraphQL.Tests.HttpTests {
  using TDict = IDictionary<string, object>;

  public static class TestEnv {
    public static string ServiceUrl = "http://127.0.0.1:55000";
    public static string GraphQLEndPointUrl = ServiceUrl + "/graphql";
    public static GraphQLHttpServer ThingsHttpServer;
    public static ThingsApi ThingsApi;
    public static RestClient RestClient;
    public static GraphQLClient Client; 

    public static string LogFilePath = "_graphQLHttpTests.log";
    private static JsonSerializerSettings _serializerSettings;


    static IWebHost _webHost;

    public static void Initialize() {
      if (ThingsHttpServer != null) //already initialized
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);

      _serializerSettings = new JsonSerializerSettings() {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
      };
      _serializerSettings.Converters.Add(new StringEnumConverter()); // enums as strings, not ints

      // create server and Http graphQL server 
      var thingsBizApp = new ThingsApp();
      ThingsApi = new ThingsApi(thingsBizApp);
      var thingsServer = new GraphQLServer(ThingsApi);
      thingsServer.Initialize();
      ThingsHttpServer = new GraphQLHttpServer(thingsServer);

      StartWebHost();
      RestClient = new RestClient(GraphQLEndPointUrl);
      Client = new GraphQLClient(GraphQLEndPointUrl);
      Client.RequestCompleted += Client_RequestCompleted;
    }

    private static void StartWebHost() {
      var hostBuilder = WebHost.CreateDefaultBuilder()
          .ConfigureAppConfiguration((context, config) => { })
          .UseStartup<TestAppStartup>()
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
      if (req.Method == RequestMethod.Get) {
        reqText = @$"GET, URL: {req.UrlQueryPartForGet} 
                unescaped: {Uri.UnescapeDataString(req.UrlQueryPartForGet)}";
      } else 
        reqText = "POST, payload: " + Environment.NewLine + GetPayloadJson(response.Request);
      // for better readability, unescape \r\n
      reqText = reqText.Replace("\\r\\n", Environment.NewLine);
      var jsonResponse = JsonConvert.SerializeObject(response.Payload, Formatting.Indented);
      var text = $@"
Request: 
{reqText}

Response:
{jsonResponse}

//  time: {response.TimeMs} ms
----------------------------------------------------------------------------------------------------------------------------------- 

";
      LogText(text);
      if (response.Exception != null)
        LogText(response.Exception.ToText());
    }

    private static string GetPayloadJson(ClientRequest request) {
      var payload = request.PostPayload;
      var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
      return json;
    }



    static object _lock = new object();
    public static void LogText(string text) {
      lock (_lock) {
        File.AppendAllText(LogFilePath, text);
      }
    }

  }
}
