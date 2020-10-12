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

namespace StarWars.Api.Tests {

  public static class TestEnv {

    public static GraphQLServer StarWarsServer;
    public static bool LogEnabled = true;
    public static string LogFilePath = "_starWarsTests.log";
    private static JsonSerializerSettings _serializerSettings;

    public static RequestContext LastRequestContext;

    public static void Init() {
      if (StarWarsServer != null)
        return;
      if (File.Exists(LogFilePath))
        File.Delete(LogFilePath);
      var app = new StarWarsApp();
      var api = new StarWarsApi(app);
      StarWarsServer = new GraphQLServer(api);
      StarWarsServer.Initialize();

      // Printout schema
      var schemaGen = new SchemaDocGenerator();
      var schemaDoc = schemaGen.GenerateSchema(api.Model);
      File.WriteAllText("_starWarsSchema.txt", schemaDoc);

      _serializerSettings = new JsonSerializerSettings() {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() }
      };
    }

    public static async Task<GraphQLResponse> ExecuteAsync(string query, IDictionary<string, object> variables = null, bool throwOnError = true) {
      GraphQLResponse resp = null;
      try {
        var req = new GraphQLRequest() { Query = query, Variables = variables };
        resp = await StarWarsServer.ExecuteAsync(req);
        LogCompletedRequest(req, resp);
        if (!resp.IsSuccess()) {
          var errText = resp.GetErrorsAsText();
          Debug.WriteLine("Errors: \r\n" + errText);
        }
      } catch (Exception ex) {
        TestEnv.LogText(ex.ToString());
        throw;
      }
      if (resp != null && resp.Errors != null && resp.Errors.Count > 0 && throwOnError)
        throw new Exception($"Request failed: {resp.Errors[0].Message}");
      return resp;
    }

    public static void LogCompletedRequest(GraphQLRequest request, GraphQLResponse response) {
      if (!LogEnabled)
        return;
      var jsonRequest = JsonConvert.SerializeObject(request, _serializerSettings);
      // for better readability, unescape \r\n
      jsonRequest = jsonRequest.Replace("\\r\\n", Environment.NewLine);
      var jsonResponse = SerializeResponse(response);
      var text = $@"
Request: 
{jsonRequest}

Response:
{jsonResponse}
";
      LogText(text);
    }

    public static void LogText(string text) {
      File.AppendAllText(LogFilePath, text);
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
