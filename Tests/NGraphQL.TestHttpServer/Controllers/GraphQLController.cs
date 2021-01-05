using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGraphQL.Server;
using NGraphQL.Server.Http;

namespace NGraphQL.TestHttpServer.Controllers {
  [ApiController]
  [Route("graphql")]
  public class GraphQLController : ControllerBase {
    GraphQLServer _server;
    JsonVariablesDeserializer _varDeserializer;

    public GraphQLController(GraphQLServer server, JsonVariablesDeserializer varDeserializer) {
      _server = server;
      _varDeserializer = varDeserializer;
      _server.Events.RequestPrepared += Events_RequestPrepared;
    }

    private void Events_RequestPrepared(object sender, GraphQLServerEventArgs e) {
      if (e.RequestContext.Operation.Variables.Count == 0)
        return;
      _varDeserializer.PrepareRequestVariables(e.RequestContext);
    }

    [HttpPost]
    public Task<object> PostAsync(GraphQLRequest request) {
      return ExecuteRequestAsync(request); 
    }
    
    [HttpGet]
    public object Get() {
      return "GET not implemented";
    }

    private async Task<object> ExecuteRequestAsync(GraphQLRequest request) {
      var resp = await _server.ExecuteAsync(request);
      object result;
      if (resp.Errors != null && resp.Errors.Count > 0)
        result = new { errors = resp.Errors, data = resp.Data };
      else
        result = new { data = resp.Data };
      return result;
    }

    [HttpGet]
    [Route("schema")]
    public string GetSchema() {
      return _server.Model.SchemaDoc;
    }
  }
}
