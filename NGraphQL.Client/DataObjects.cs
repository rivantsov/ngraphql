using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace NGraphQL.Client {

  public enum RequestType {
    Get,
    Post,
  }

  [Flags]
  public enum RequestOptions {
    None = 0,
    MultiPart = 1, 
    Chunked = 1 << 1, 

  }

  public class ClientRequest {
    public RequestType RequestType;
    public string Query;
    public string OperationName; 
    public IDictionary<string, object> Variables;
    public IDictionary<string, object> PostPayload;
    public string GetUrlQuery;
    public Type ResultType;
    public CancellationToken CancellationToken;
    public IDictionary<string, string> Headers;
    public HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead;
  }

  public class ServerResponse {
    public ClientRequest Request;
    public IDictionary<string, object> Payload; 
    public IList<ServerError> Errors;
    public dynamic Data;
    public int TimeMs;
    public Exception Exception; 
  }

  public class ServerError {
    public string Message;
    public IList<Location> Locations = new List<Location>();
    public IList<object> Path;
    public IDictionary<string, object> Extensions = new Dictionary<string, object>();

    public override string ToString() {
      var str = Message;
      if (Path != null)
        str += " path: [" + string.Join(", ", Path) + "]";
      if (Locations != null && Locations.Count > 0)
        str += $" at: {Locations[0]}";
      return str;
    }
  }

  public class Location {
    public int Line;
    public int Column;
    public override string ToString() => $"({Line}, {Column})";
  }

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

