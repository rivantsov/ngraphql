using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NGraphQL.Server.AspNetCore {
  public class DefaultGraphQLController: Controller {
    GraphQLHttpHandler _handler;
  
    public DefaultGraphQLController(GraphQLHttpHandler handler) {
      _handler = handler;
    }

    [HttpPost, HttpGet, Route("graphql")]
    public async Task HandleRequest() {
      await _handler.HandleGraphQLHttpRequestAsync(this.HttpContext);
    }

    [HttpGet, Route("graphql/schema")]
    public string GetSchema() {
      return _handler.Server.Model.SchemaDoc;
    }

  }
}
