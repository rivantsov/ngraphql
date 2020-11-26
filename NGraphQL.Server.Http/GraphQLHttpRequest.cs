using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Http {

  public enum HttpContentType {
    None,
    Json,
    GraphQL,
  }

  public class GraphQLHttpRequest {
    public HttpContext HttpContext;
    public string HttpMethod;
    public HttpContentType ContentType;
    // these are variables initially deserialized from json as dictionary; 
    // values contain Json objects like JArray and JToken; we later parse these, unpack json objects
    // and put values into request.Variables
    public IDictionary<string, object> RawVariables;

    public GraphQLRequest Request;
    public RequestContext RequestContext;
    public Exception Exception;

    public RequestMetrics Metrics => RequestContext.Metrics; 
  }

}
