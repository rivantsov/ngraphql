using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace NGraphQL.Client.Types {

  public class ClientRequest {
    public GraphQLRequest Body;
    public string BodyJson;

    public string HttpMethod;
    public string UrlQueryPartForGet;
    public Type ResultType;
    public CancellationToken CancellationToken;
    public IDictionary<string, string> Headers;

    public ClientRequest() { }
  }

}

