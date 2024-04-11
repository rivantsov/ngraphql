using Microsoft.AspNetCore.Http;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.AspNetCore {

  public class GraphQLHttpRequest {
    public HttpContext HttpContext;
    public string HttpMethod;
    public HttpContentType ContentType;

    public GraphQLRequest Request;
    public RequestContext RequestContext;
  }

  public enum HttpContentType {
    None,
    Json,
    GraphQL,
  }

}
