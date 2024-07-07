using System;

namespace NGraphQL.Client.Types {

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


}

