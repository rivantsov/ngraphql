using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

using NGraphQL.Client;
using NGraphQL.Utilities;
using Things.GraphQL.HttpServer;

namespace NGraphQL.Tests.HttpTests {

  public static class TestEnv {
    public static string ServiceUrl = "http://127.0.0.1:55571";
    public static string GraphQLEndPointUrl = ServiceUrl + "/graphql";
    public static GraphQLClient Client;
    static Task _task; 
    public static string LogFilePath = "_graphQLHttpTests.log";

    public static void Initialize() {
      if (Client != null) //already initialized
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);

      Client = new GraphQLClient(GraphQLEndPointUrl);
      Client.RequestCompleted += Client_RequestCompleted;

      _task = ThingsWebServerStartupHelper.StartThingsGraphqQLWebServer(args: null, enablePreviewFeatures: true, useGraphiql: false, serverUrl: ServiceUrl);
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
      LogCompletedRequest(e.Result);
    }

    public static void LogCompletedRequest(GraphQLResult result) {
      string reqText;
      var req = result.Request; 
      if (req.HttpMethod == "GET") {
        reqText = @$"GET, URL: {req.UrlQueryPartForGet} 
                unescaped: {Uri.UnescapeDataString(req.UrlQueryPartForGet)}";
      } else {
        // for better readability, unescape \r\n; Json serializer escapes new-line symbols inside strings,
        var bodyUnesc = req.Body.Replace("\\r\\n", Environment.NewLine);
        reqText = "POST, payload: " + Environment.NewLine + bodyUnesc;
      }
      var jsonResponse = JsonConvert.SerializeObject(result.TopFields, Formatting.Indented);
      var text = $@"
Request: 
{reqText}

Response:
{jsonResponse}

//  time: {result.DurationMs} ms
----------------------------------------------------------------------------------------------------------------------------------- 

";
      LogText(text);
      if (result.Exception != null)
        LogText(result.Exception.ToText());
    }


    static object _lock = new object();
    public static void LogText(string text) {
      lock (_lock) {
        File.AppendAllText(LogFilePath, text);
      }
    }

  }
}
