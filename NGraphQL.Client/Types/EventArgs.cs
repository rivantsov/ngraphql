using System;

namespace NGraphQL.Client {

  public class RequestStartingEventArgs : EventArgs {
    public readonly ClientRequest Request;

    public RequestStartingEventArgs(ClientRequest request) {
      Request = request; 
    }
  }

  public class RequestCompletedEventArgs : EventArgs {
    public readonly ServerResponse Response;

    public RequestCompletedEventArgs(ServerResponse response) {
      Response = response; 
    }
  }


}

