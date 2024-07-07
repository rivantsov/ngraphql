using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Subscriptions;

namespace NGraphQL.Server; 

public class GraphQLServerEvents {
  public event EventHandler<GraphQLServerEventArgs> RequestStarting;
  public event EventHandler<GraphQLServerEventArgs> RequestPrepared;
  public event EventHandler<GraphQLServerEventArgs> RequestCompleted;
  public event EventHandler<OperationErrorEventArgs> OperationError;
  public event EventHandler<GraphQLServerEventArgs> RequestError;
  public event EventHandler<SubscriptionEventArgs> SubscriptionAction;
  public event EventHandler<SubscriptionEventArgs> SubscriptionActionError;
  public event EventHandler<SubscriptionPublishEventArgs> SubscriptionPublishError;

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

  internal void OnSubscriptionAction(SubscriptionContext context) {
    SubscriptionAction?.Invoke(this, new SubscriptionEventArgs(context));
  }
  internal void OnSubscriptionActionError(SubscriptionContext context) {
    SubscriptionActionError?.Invoke(this, new SubscriptionEventArgs(context));
  }
  internal void OnSubscriptionPublishError(PublishContext context) {
    SubscriptionPublishError?.Invoke(this, new SubscriptionPublishEventArgs() {Context = context });
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

public class SubscriptionEventArgs: EventArgs {
  public SubscriptionContext Context;
  public SubscriptionEventArgs(SubscriptionContext ctx) {
    Context = ctx; 
  }
}

public class SubscriptionPublishEventArgs : EventArgs {
  public PublishContext Context; 
}

