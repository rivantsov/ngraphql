using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Http {

  public class GraphQLHttpServer {
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeGraphQL = "application/graphql";

    public readonly JsonSerializerSettings SerializerSettings; 
    public readonly GraphQLServer Server;
    public readonly HttpServerEvents Events = new HttpServerEvents();
    public readonly GraphQLHttpOptions Options; 
    JsonVariablesDeserializer _varDeserializer;

    public GraphQLHttpServer(GraphQLServer server, JsonSerializerSettings serializerSettings = null, 
               GraphQLHttpOptions options = GraphQLHttpOptions.ReturnExceptionDetails) {
      Server = server;
      Options = options; 
      SerializerSettings = serializerSettings; 
      if (SerializerSettings == null) {
        var stt = SerializerSettings = new JsonSerializerSettings();
        stt.Formatting = Formatting.Indented;
        stt.ContractResolver = new DefaultContractResolver {
          NamingStrategy = new CamelCaseNamingStrategy()
        };
        SerializerSettings.MaxDepth = 50;
     }
      _varDeserializer = new JsonVariablesDeserializer(); 
      // hook to RequestPrepared to deserialize variables after query is parsed and var types are known
      Server.Events.RequestPrepared += Server_RequestPrepared;
    }

    public async Task HandleGraphQLHttpRequestAsync(HttpContext httpContext) {
      if (httpContext.Request.Path.Value.EndsWith("/schema")) {
        await HandleSchemaDocRequestAsync(httpContext);
        return;
      }
      GraphQLHttpRequest gqlHttpReq = null;
      var start = AppTime.GetTimestamp();
      gqlHttpReq = BuildGraphQLHttpRequest(httpContext);
      Events.OnRequestStarting(gqlHttpReq);
      try {
        await Server.ExecuteRequestAsync(gqlHttpReq.RequestContext);
      } catch (Exception exc) {
        gqlHttpReq.RequestContext.Exceptions.Add(exc);
        Events.OnRequestError(gqlHttpReq);
      }

      // check errors
      var reqCtx = gqlHttpReq.RequestContext; //internal request context
      if (reqCtx.Exceptions.Count > 0) {
        await WriteExceptionsAsTextAsync(httpContext, reqCtx.Exceptions);
        return;
      }
      // success,  serialize response
      try {
        var httpResp = httpContext.Response;
        httpResp.ContentType = ContentTypeJson;
        if (Options.IsSet(GraphQLHttpOptions.SuppressChunking))
          httpResp.Headers["Transfer-Encoding"] = "identity"; //to disable chunking
        var respJson = SerializeResponse(reqCtx.Response);
        await httpResp.WriteAsync(respJson, httpContext.RequestAborted);
        reqCtx.Metrics.HttpRequestDuration = AppTime.GetDuration(start);
        Events.OnRequestCompleted(gqlHttpReq); 
      } catch (Exception ex) {
        await WriteExceptionsAsTextAsync(httpContext, new[] { ex });
        //create new if not yet created, just to report exc in event
        gqlHttpReq = gqlHttpReq ?? new GraphQLHttpRequest() { HttpContext = httpContext }; 
        Events.OnRequestError(gqlHttpReq);
      }
    }

    private async Task HandleSchemaDocRequestAsync(HttpContext context) {
      context.Response.ContentType = "application/text";
      await context.Response.WriteAsync(Server.Model.SchemaDoc);
    }

    // request is parsed; now we know input variable types, we can deserialize them. 
    private void Server_RequestPrepared(object sender, GraphQLServerEventArgs e) {
      if (e.RequestContext.Operation.Variables.Count == 0)
        return; 
      _varDeserializer.PrepareRequestVariables(e.RequestContext);
    }

    private async Task WriteExceptionsAsTextAsync(HttpContext context, IList<Exception> exs) {
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      var excText = string.Join(Environment.NewLine, exs);
      context.Response.ContentType = "application/text";
      await context.Response.WriteAsync(excText);
    }

    // see https://graphql.org/learn/serving-over-http/#http-methods-headers-and-body
    public GraphQLHttpRequest BuildGraphQLHttpRequest(HttpContext httpContext) {
      GraphQLHttpRequest gqlHttpReq; 
      var method = httpContext.Request.Method;
      switch (method) {
        case "GET":
          gqlHttpReq = BuildGetRequest(httpContext);
          break; 
        case "POST":
          gqlHttpReq = BuildPostRequest(httpContext);
          break; 
        default:
          throw new Exception($"Invalid Http method: {method}; expected GET or POST.");
      }
      // create internal request context
      gqlHttpReq.RequestContext =
            Server.CreateRequestContext(gqlHttpReq.Request, httpContext.RequestAborted,
                    httpContext.User, null, gqlHttpReq);
      return gqlHttpReq;
    }

    private GraphQLHttpRequest BuildGetRequest(HttpContext httpContext) {
      var uriQuery = httpContext.Request.Query;      
      var httpReq = new GraphQLHttpRequest() {
        HttpContext = httpContext,
        HttpMethod = "GET",   
        Request = new GraphQLRequest() {
          Query = uriQuery["query"],
          OperationName = uriQuery["operationName"]
        }, 
        ContentType = HttpContentType.None, //no body
      };
      var varsJson = uriQuery["variables"];
      if (!string.IsNullOrWhiteSpace(varsJson)) 
        httpReq.RawVariables = Deserialize<IDictionary<string, object>>(varsJson);
      return httpReq; 
    }

    private GraphQLHttpRequest BuildPostRequest(HttpContext httpContext) {
      var req = new GraphQLRequest();
      var httpReq = new GraphQLHttpRequest() {
        HttpContext = httpContext, 
        HttpMethod = "POST", 
        Request = req };
      // read body
      var reader = new StreamReader(httpContext.Request.Body);
      var body = reader.ReadToEnd();
      httpReq.ContentType = GetRequestContentType(httpContext.Request);
      switch(httpReq.ContentType) {
          
        case HttpContentType.GraphQL:
          req.Query = body;
          break;

        case HttpContentType.Json:
          var bodyObj = Deserialize<GraphQLRequest>(body);
          req.Query = bodyObj.Query;
          req.OperationName = bodyObj.OperationName;
          httpReq.RawVariables = bodyObj.Variables;
          break; 
      }
      // still check 'query' parameter in URL and overwrite query in body if found
      var urlQry = httpContext.Request.Query;
      var urlQryPrm = urlQry["query"];
      if (!string.IsNullOrEmpty(urlQryPrm))
        req.Query = urlQryPrm;
      
      return httpReq; 
    }//method

    private HttpContentType GetRequestContentType(HttpRequest request) {
      var contTypeStr = request.Headers["Content-Type"].FirstOrDefault();
      switch (contTypeStr) {
        case ContentTypeGraphQL: return HttpContentType.GraphQL;
        case ContentTypeJson:
        default:
          return HttpContentType.Json;
      }
    }

    private T Deserialize<T>(string json) {
      return JsonConvert.DeserializeObject<T>(json, SerializerSettings); 
    }

    private string SerializeResponse(GraphQLResponse response) {
      object respObj; 
      // we put data in auto-object, to achieve some special output json formatting 
      // - to force lower-case field names (we could use CamelCase name policy but this is simpler
      // - to make sure errors key does not appear in the output (with value null) when there are no errors
      //    and if there are errors they appear first. 
      if(response.Errors.Count  == 0)
        respObj = new { data = response.Data };
      else 
        respObj = new { errors = response.Errors, data = response.Data };

      var json = JsonConvert.SerializeObject(respObj, SerializerSettings);
      return json; 
    }
   


  }
}
