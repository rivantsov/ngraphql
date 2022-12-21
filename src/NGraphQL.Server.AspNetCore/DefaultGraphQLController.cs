using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NGraphQL.Server.AspNetCore {
  public class DefaultGraphQLController: Controller {
    GraphQLHttpHandler _server;
  
    public DefaultGraphQLController(GraphQLHttpHandler server) {
      _server = server;
    }

    [HttpPost, HttpGet, Route("graphql")]
    public async Task HandleRequest() {
      await _server.HandleGraphQLHttpRequestAsync(this.HttpContext);
    }

    [HttpGet, Route("graphql/schema")]
    public string GetSchema() {
      return _server.Server.Model.SchemaDoc;
    }

  }
}
