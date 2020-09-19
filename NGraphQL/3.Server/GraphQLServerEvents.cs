using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server {

  public class GraphQLServerEventArgs : EventArgs {
    public readonly RequestContext RequestContext;
    public GraphQLServerEventArgs(RequestContext context) {
      RequestContext = context;
    }
  }

  public class GraphQLServerEvents {
    public event EventHandler<GraphQLServerEventArgs> RequestStarting;
    public event EventHandler<GraphQLServerEventArgs> RequestPrepared;
    public event EventHandler<GraphQLServerEventArgs> RequestCompleted;
    public event EventHandler<GraphQLServerEventArgs> RequestError;

    internal GraphQLServerEvents () { }

    // events ----------------------- 
    internal void OnRequestStarting(RequestContext context) {
      RequestStarting?.Invoke(this, new GraphQLServerEventArgs(context));
    }
    
    internal void OnRequestPrepared(RequestContext context) {
      RequestPrepared?.Invoke(this, new GraphQLServerEventArgs(context));
    }
    
    internal void OnRequestCompleted(RequestContext context) {
      RequestCompleted?.Invoke(this, new GraphQLServerEventArgs(context));
    }
    
    internal void OnRequestError(RequestContext context) {
      RequestError?.Invoke(this, new GraphQLServerEventArgs(context));
    }
  }
}
