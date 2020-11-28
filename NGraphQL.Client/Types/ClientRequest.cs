using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace NGraphQL.Client {

  public class ClientRequest {
    public string HttpMethod;


    public string Query;
    public string OperationName; 
    public IDictionary<string, object> Variables;

    public IDictionary<string, object> PostPayload;
    public string UrlQueryPartForGet;
    public Type ResultType;
    public CancellationToken CancellationToken;
    public IDictionary<string, string> Headers;
    public HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead;
  }

}

