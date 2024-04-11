using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NGraphQL.Json;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server.AspNetCore {

  /// <summary>GraphQL Http Server. </summary>
  public class GraphQLHttpHandler {
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeGraphQL = "application/graphql";
    public const string OperationContextKey = "_vita_operation_context_"; // When using VitaWebMiddleware

    JsonSerializerOptions _basicJsonOptionsNoConverters;
    JsonSerializerOptions _jsonOptionsForSerializer;
    public readonly GraphQLServer Server;
    JsonVariablesDeserializer _varDeserializer;

    public GraphQLHttpHandler(GraphQLServer server) {
      Server = server;
      if (Server.Model == null)
        Server.Initialize();
      _basicJsonOptionsNoConverters = new JsonSerializerOptions() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
      };
      _jsonOptionsForSerializer = JsonDefaults.JsonOptions; 
      _varDeserializer = new JsonVariablesDeserializer(); 
      // hook to RequestPrepared to deserialize variables after query is parsed and var types are known
      Server.Events.RequestPrepared += Server_RequestPrepared;
    }

    public async Task HandleGraphQLHttpRequestAsync(HttpContext httpContext) {
      if (httpContext.Request.Path.Value.EndsWith("/schema")) {
        await HandleSchemaDocRequestAsync(httpContext);
        return;
      }
      var start = AppTime.GetTimestamp();
      var gqlHttpReq = await BuildGraphQLHttpRequestAsync(httpContext);
      var reqCtx = gqlHttpReq.RequestContext; //internal request context

      try {
        await Server.ExecuteRequestAsync(gqlHttpReq.RequestContext);
      } catch (Exception exc) {
        gqlHttpReq.RequestContext.AddError(exc);
      }

      // success,  serialize response
      try {
        var httpResp = httpContext.Response;
        httpResp.ContentType = ContentTypeJson;
        var respJson = SerializeResponse(reqCtx.Response);
        await httpResp.WriteAsync(respJson, httpContext.RequestAborted);
        reqCtx.Metrics.HttpRequestDuration = AppTime.GetDuration(start);
      } catch (Exception ex) {
        // this ex is at attempt to write response as json; we try to write it as plain text and return something
        await WriteExceptionsAsTextAsync(httpContext, new[] { ex });
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
    private async Task<GraphQLHttpRequest> BuildGraphQLHttpRequestAsync(HttpContext httpContext) {
      GraphQLHttpRequest gqlHttpReq;  
      var method = httpContext.Request.Method;
      switch (method) {
        case "GET":
          gqlHttpReq = BuildGetRequest(httpContext);
          break; 
        case "POST":
          gqlHttpReq = await BuildPostRequestAsync(httpContext);
          break; 
        default:
          throw new Exception($"Invalid Http method: {method}; expected GET or POST.");
      }
      // create internal request context
      gqlHttpReq.RequestContext =
            Server.CreateRequestContext(gqlHttpReq.Request, httpContext.RequestAborted,
                    httpContext.User, null, httpContext);
      // Try retrieving VITA operation context. 
      if (httpContext.Items.TryGetValue(OperationContextKey, out object opContext))
        gqlHttpReq.RequestContext.VitaOperationContext = opContext; 
      return gqlHttpReq;
    }

    private GraphQLHttpRequest BuildGetRequest(HttpContext httpContext) {
      var uriQuery = httpContext.Request.Query;
      var gqlRequest = new GraphQLRequest() {
        Query = uriQuery["query"],
        OperationName = uriQuery["operationName"]
      };
      var varsJson = uriQuery["variables"];
      if (!string.IsNullOrWhiteSpace(varsJson))
        // note: we do not want to deserialize each var yet, we do it later when we know their types
        // for now we want just Dict<string, JsonElement>; that's why we use JsonOptions without converters
        gqlRequest.Variables = JsonSerializer.Deserialize<IDictionary<string, object>>(varsJson, this._basicJsonOptionsNoConverters);
      var httpReq = new GraphQLHttpRequest() {
        HttpContext = httpContext,
        HttpMethod = "GET",
        Request = gqlRequest,
        ContentType = HttpContentType.None, //no body
      };
      return httpReq; 
    }

    private async Task<GraphQLHttpRequest> BuildPostRequestAsync(HttpContext httpContext) {
      var req = new GraphQLRequest();
      var httpReq = new GraphQLHttpRequest() {
        HttpContext = httpContext, 
        HttpMethod = "POST", 
        Request = req };
      // read body
      var reader = new StreamReader(httpContext.Request.Body);
      var body = await reader.ReadToEndAsync();
      httpReq.ContentType = GetRequestContentType(httpContext.Request);
      switch(httpReq.ContentType) {
          
        case HttpContentType.GraphQL:
          req.Query = body;
          break;

        case HttpContentType.Json:
          var bodyObj = JsonSerializer.Deserialize<GraphQLRequest>(body, this._basicJsonOptionsNoConverters);
          req.Query = bodyObj.Query;
          req.OperationName = bodyObj.OperationName;
          req.Variables = bodyObj.Variables;
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

      var json = JsonSerializer.Serialize(respObj, _jsonOptionsForSerializer);
      return json; 
    }
   


  }
}
