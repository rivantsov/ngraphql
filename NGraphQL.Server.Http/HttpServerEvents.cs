using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Http {

  public class HttpRequestEventArgs : EventArgs {
    public readonly GraphQLHttpRequest Request;

    public HttpRequestEventArgs(GraphQLHttpRequest request) {
      Request = request;
    }
  }

  public class HttpServerEvents {
    public event EventHandler<HttpRequestEventArgs> RequestStarting;
    public event EventHandler<HttpRequestEventArgs> RequestCompleted;
    public event EventHandler<HttpRequestEventArgs> RequestError;

    internal HttpServerEvents() { }

    // events ----------------------- 
    internal void OnRequestStarting(GraphQLHttpRequest request) {
      RequestStarting?.Invoke(this, new HttpRequestEventArgs(request));
    }

    internal void OnRequestCompleted(GraphQLHttpRequest request) {
      RequestCompleted?.Invoke(this, new HttpRequestEventArgs(request));
    }

    internal void OnRequestError(GraphQLHttpRequest request) {
      RequestError?.Invoke(this, new HttpRequestEventArgs(request));
    }
  }

}
