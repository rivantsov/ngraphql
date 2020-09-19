﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Arrest;
using Arrest.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using NGraphQL.Http;
using NGraphQL.Server;
using NGraphQL.TestApp;
using NGraphQL.Utilities;

namespace NGraphQL.Tests.HttpTests {
  using TDict = IDictionary<string, object>;

  public static class TestEnv {
    public static string ServiceUrl = "http://127.0.0.1:5900";
    public static GraphQLHttpServer ThingsHttpServer;
    public static ThingsApi ThingsApi;
    public static RestClient Client;
    public static GraphQLHttpRequest LastServerSideRequestObject;
    public static TimeSpan LastRequestDuration; // measured on the client

    public static string LogFilePath = "_graphQLHttpTests.log";
    private static JsonSerializerSettings _serializerSettings;


    static IWebHost _webHost;

    public static void Initialize () {
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
      ThingsHttpServer.Events.RequestCompleted += ThingsHttpServer_RequestCompleted;

      StartWebHost(); 
      Client = new RestClient(ServiceUrl + "/graphql");
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

    public static async Task<GraphQLResponse> SendAsync(string query, IDictionary<string, object> vars = null, 
         string opName = null, bool throwOnError = true) {
      var start = AppTime.GetTimestamp(); 
      var reqDict = new Dictionary<string, object>();
      reqDict["query"] = query;
      if (vars != null)
        reqDict["variables"] = vars;
      if (opName != null)
        reqDict["operationName"] = opName;
      // we cannot get response directly as GraphQLResponse because casing ('data' in json vs Data prop in GraphQLResponse)
      //  so we get it as stream and deserialize using custom settings (with camel case naming policy)
      var respStream = await Client.PostAsync<TDict, Stream>(reqDict, string.Empty);
      // read response
      var reader = new StreamReader(respStream);
      var respBody = reader.ReadToEnd();
      var resp = JsonConvert.DeserializeObject<GraphQLResponse>(respBody, _serializerSettings);
      LastRequestDuration = AppTime.GetDuration(start);
      LogCompletedRequest(reqDict, LastServerSideRequestObject); 

      if (throwOnError && resp.Errors != null && resp.Errors.Count > 0)
        throw new Exception("Server returned error: " + resp.Errors[0].Message);
      return resp; 
    }

    private static void ThingsHttpServer_RequestCompleted(object sender, HttpRequestEventArgs e) {
      // we hook to server event to catch the request context data here;
      // we need server-side metrics info, which we won't get in the regular response - it will contain just json data.
      // We do not log to file here, to avoid impacting client-side metrics (total time for the client). 
      // So we just save it, and SendAsync method will print it all after client completes the request. 
      LastServerSideRequestObject = e.Request;
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

    public static void LogText(string text) {
      File.AppendAllText(LogFilePath, text);
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

    public static void LogCompletedRequest(IDictionary<string, object> reqDict, GraphQLHttpRequest serverReqData) {
      var reqCtx = serverReqData.RequestContext;
      var mx = reqCtx.Metrics;
      var jsonReq = JsonConvert.SerializeObject(reqDict, _serializerSettings); 
      // for better readability, unescape \r\n
      jsonReq = jsonReq.Replace("\\r\\n", Environment.NewLine);
      var jsonResponse = SerializeResponse(reqCtx.Response);
      var text = $@"
Request: 
{jsonReq}

Response:
{jsonResponse}

//  client time: {LastRequestDuration.TotalMilliseconds} ms, HTTP server execution time: {mx.HttpRequestDuration.TotalMilliseconds} ms
//  request from cache: {mx.FromCache}, threads: {mx.ExecutionThreadCount}, resolver calls: {mx.ResolverCallCount}, output objects: {mx.OutputObjectCount}
----------------------------------------------------------------------------------------------------------------------------------- 

";
      LogText(text);
      foreach (var ex in reqCtx.Exceptions)
        LogText(ex.ToText());
    }

  }
}