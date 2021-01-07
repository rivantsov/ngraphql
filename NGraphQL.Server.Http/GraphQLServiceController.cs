using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Http {

  [ApiController]
  [Route("graphql")]
  public class GraphQLController : ControllerBase {
    GraphQLHttpServer _server;

    public GraphQLController(GraphQLHttpServer server) {
      _server = server;
    }

    [HttpPost,  HttpGet]
    [Route("")]
    public Task HandleRequest() {
      return _server.HandleGraphQLHttpRequestAsync(this.HttpContext); 
    }

  }
}
