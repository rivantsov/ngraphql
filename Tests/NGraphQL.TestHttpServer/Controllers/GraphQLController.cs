using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGraphQL.Server;

namespace NGraphQL.TestHttpServer.Controllers {
  [ApiController]
  [Route("graphql")]
  public class GraphQLController : ControllerBase {
    GraphQLServer _server; 

    public GraphQLController(GraphQLServer server) {
      _server = server; 
    }

    [HttpPost]
    public async Task<object> PostAsync([FromBody] GraphQLRequest request) {
      var resp = await _server.ExecuteAsync(request);
      object result;
      if (resp.Errors != null && resp.Errors.Count > 0)
        result = new { errors = resp.Errors, data = resp.Data};
      else 
        result = new { data = resp.Data};
      return result; 
    }
    
    [HttpGet]
    public object Get() {
      return "GET not implemented";
    }

    [HttpGet]
    [Route("schema")]
    public string GetSchema() {
      return _server.Model.SchemaDoc;
    }
  }
}
