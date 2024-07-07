using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.Client;
using NGraphQL.Client.Types;
using NGraphQL.Utilities;
using Things.GraphQL.HttpServer;

namespace NGraphQL.Tests.HttpTests {

  public static class TestEnv {
    public static string ServiceUrl = "http://127.0.0.1:55571";
    public static string GraphQLEndPointUrl = ServiceUrl + "/graphql";
    public static GraphQLClient Client;
    public static string LogFilePath = "_graphQLHttpTests.log";

    public static void Initialize() {
      if (Client != null) //already initialized
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);
      // start server
      var task = TestServerStartup.SetupServer(args: null, enablePreviewFeatures: true, serverUrl: ServiceUrl);
      Thread.Sleep(50);
      // setup client
      Client = new GraphQLClient(GraphQLEndPointUrl, enableSubscriptions: true);
      Client.OnError += Client_OnError;
      Client.RequestCompleted += Client_RequestCompleted;
    }

    private static void Client_OnError(object sender, RequestErrorEventArgs e) {
      var text = @$"
================================  GraphQLClient reported error ================================
 {e.Exception}
Additional info: 
{e.Information}    

";
      LogText(text);
      Trace.WriteLine(text); 
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
        var bodyUnesc = req.BodyJson.Replace("\\r\\n", Environment.NewLine);
        reqText = bodyUnesc;
      }
      var text = $@"
HttpMethod: {req.HttpMethod}
Request: 
{reqText}

Response:
{result.ResponseJson}

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
