using System;
using NGraphQL.Subscriptions;

namespace NGraphQL.Client.Types; 

public class RequestStartingEventArgs : EventArgs {
  public readonly ClientRequest Request;

  public RequestStartingEventArgs(ClientRequest request) {
    Request = request; 
  }
}

public class RequestCompletedEventArgs : EventArgs {
  public readonly GraphQLResult Result;

  public RequestCompletedEventArgs(GraphQLResult result) {
    Result = result; 
  }
}

public class RequestErrorEventArgs : EventArgs {
  public readonly Exception Exception;
  public readonly string Information;

  public RequestErrorEventArgs(Exception exc, string info) {
    Exception = exc;
    Information = info;
  }
}

public class SubscriptionMessageEventArgs : EventArgs {
  public readonly SubscriptionMessage Message;

  public SubscriptionMessageEventArgs(SubscriptionMessage message) {
    Message = message;
  }
}
public class ErrorMessageEventArgs : EventArgs {
  public readonly ErrorMessage Message;

  public ErrorMessageEventArgs(ErrorMessage message) {
    Message = message;
  }
}



