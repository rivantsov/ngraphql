using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Http {

  public class GraphQLHttpRequest {
    public HttpContext HttpContext;
    public string HttpMethod;
    public HttpContentType ContentType;

    public GraphQLRequest Request;
    public RequestContext RequestContext;

    public RequestMetrics Metrics => RequestContext.Metrics; 
  }

}
