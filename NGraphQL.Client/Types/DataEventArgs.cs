using System;

namespace NGraphQL.Client {

  public class RequestStartingEventArgs : EventArgs {
    public readonly RequestData Data;

    public RequestStartingEventArgs(RequestData data) {
      Data = data; 
    }
  }

  public class RequestCompletedEventArgs : EventArgs {
    public readonly ResponseData Data;

    public RequestCompletedEventArgs(ResponseData data) {
      Data = data; 
    }
  }


}

