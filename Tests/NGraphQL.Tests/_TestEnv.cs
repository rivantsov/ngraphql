using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;
using NGraphQL.TestApp;

namespace NGraphQL.Tests {

  public static class TestEnv {

    public static GraphQLServer ThingsServer;
    public static bool LogEnabled = true;
    public static string LogFilePath = "_graphQLtests.log";
    private static JsonSerializerSettings _serializerSettings;

    public static RequestContext LastRequestContext;

    public static void Init() {
      if(ThingsServer != null)
        return;
      if(File.Exists(LogFilePath))
        File.Delete(LogFilePath);
      try {
        var thingsBizApp = new ThingsApp();
        var thingsModule = new ThingsGraphQLModule();
        ThingsServer = new GraphQLServer(thingsBizApp);
        ThingsServer.RegisterModules(thingsModule);
        ThingsServer.Initialize();
        ThingsServer.Events.RequestCompleted += ThingsServer_RequestCompleted;
      } catch (ServerStartupException sEx) {
        LogText(sEx.ToText() + Environment.NewLine);
        LogText(sEx.GetErrorsAsText());
        throw;
      }
      // Printout
      var schemaGen = new SchemaDocGenerator();
      var schemaDoc = schemaGen.GenerateSchema(ThingsServer.Model);
      File.WriteAllText("_thingsApiSchema.txt", schemaDoc);

      _serializerSettings = new JsonSerializerSettings() {
        Formatting = Formatting.Indented, 
        ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
      };
    }

    private static void ThingsServer_RequestCompleted(object sender, GraphQLServerEventArgs e) {
      LastRequestContext = e.RequestContext;
      LogCompletedRequest(e.RequestContext);
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

    public static void LogCompletedRequest(RequestContext context) {
      if(!LogEnabled)
        return;
      var mx = context.Metrics;
      var jsonRequest = JsonConvert.SerializeObject(context.RawRequest, _serializerSettings);
      // for better readability, unescape \r\n
      jsonRequest = jsonRequest.Replace("\\r\\n", Environment.NewLine);
      var jsonResponse = SerializeResponse(context.Response);
      var text = $@"
Request: 
{jsonRequest}

Response:
{jsonResponse}

// execution time: {mx.Duration.TotalMilliseconds} ms, request from cache: {mx.FromCache}, threads: {mx.ExecutionThreadCount}, " + 
$@" resolver calls: {mx.ResolverCallCount}, output objects: {mx.OutputObjectCount}
----------------------------------------------------------------------------------------------------------------------------------- 

";
      LogText(text);
      foreach(var ex in context.Exceptions)
        LogText(ex.ToText());
    }

    public static void LogText(string text) {
      File.AppendAllText(LogFilePath, text);
    }


    public static void LogException(string query, Exception ex) {
      var text = $@"

!!! Exception !!! ----------------------------------------------------------------      
{ex.ToString()}

Failed request:
{query}
";
      File.AppendAllText(LogFilePath, text);
    }

    public static async Task<GraphQLResponse> ExecuteAsync(string query, IDictionary<string, object> variables = null,bool throwOnError = true) {
      GraphQLResponse resp = null;       
      try {
        var req = new GraphQLRequest() { Query = query, Variables = variables };
        resp = await ThingsServer.ExecuteAsync(req);
        if (!resp.IsSuccess()) {
          var errText = resp.GetErrorsAsText();
          Debug.WriteLine("Errors: \r\n" + errText);
        }
      } catch(Exception ex) {
        TestEnv.LogException(query, ex);
        throw;
      }
      if (resp != null && resp.Errors != null && resp.Errors.Count > 0 && throwOnError)
        throw new Exception($"Request failed: {resp.Errors[0].Message}");
      return resp;
    }

    // Serialization for logging 
    private static string SerializeResponse(GraphQLResponse response) {
      try {
        if (response.Errors.Count > 0)
          return JsonConvert.SerializeObject(response, _serializerSettings);
        else
          return JsonConvert.SerializeObject(new { response.Data }, _serializerSettings);
      } catch (Exception ex) {
        var errText = "FATAL: " + ex.ToString();
        LogText(errText);
        return errText; 
      }
    }

  }
}
