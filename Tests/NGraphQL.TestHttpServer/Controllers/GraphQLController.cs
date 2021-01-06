using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Http;

namespace NGraphQL.TestHttpServer.Controllers {
  [ApiController]
  [Route("graphql")]
  public class GraphQLController : ControllerBase {
    GraphQLServer _server;

    public GraphQLController(GraphQLServer server) {
      _server = server;
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
      var ctx = this.HttpContext;
      var requestContext = new RequestContext(_server, request, ctx.RequestAborted, ctx.User);
      await _server.ExecuteRequestAsync(requestContext);
      var resp = requestContext.Response; 
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
