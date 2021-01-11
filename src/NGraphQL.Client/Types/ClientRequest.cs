using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace NGraphQL.Client {

  public class ClientRequest {
    public GraphQLRequest CoreRequest;

    public string HttpMethod;
    public string Body;
    public string UrlQueryPartForGet;
    public Type ResultType;
    public CancellationToken CancellationToken;
    public IDictionary<string, string> Headers;
    public HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead;

    public ClientRequest() { }
  }

}

