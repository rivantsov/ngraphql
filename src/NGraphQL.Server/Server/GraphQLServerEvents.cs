using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server {

  public class GraphQLServerEvents {
    public event EventHandler<GraphQLServerEventArgs> RequestStarting;
    public event EventHandler<GraphQLServerEventArgs> RequestPrepared;
    public event EventHandler<GraphQLServerEventArgs> RequestCompleted;
    public event EventHandler<OperationErrorEventArgs> OperationError;
    public event EventHandler<GraphQLServerEventArgs> RequestError;

    internal GraphQLServerEvents() { }

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

    internal void OnOperationError(OperationErrorEventArgs args) {
      OperationError?.Invoke(this, args);
    }

    internal void OnRequestError(RequestContext context) {
      RequestError?.Invoke(this, new GraphQLServerEventArgs(context));
    }
  }

  public class GraphQLServerEventArgs : EventArgs {
    public readonly RequestContext RequestContext;
    internal GraphQLServerEventArgs(RequestContext context) {
      RequestContext = context;
    }
  }

  public class OperationErrorEventArgs: EventArgs {
    public readonly RequestContext RequestContext;
    public readonly RequestObjectBase RequestItem; 
    public Exception Exception { get; private set; }

    public OperationErrorEventArgs(RequestContext context, RequestObjectBase requestItem, Exception exc) {
      this.RequestContext = context;
      RequestItem = requestItem;
      Exception = exc; 
    }

    public void ClearException() {
      Exception = null; 
    }
    public void SetException(Exception ex) {
      Exception = ex; 
    }

  }
}
