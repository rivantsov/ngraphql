using System;
using System.Collections.Generic;
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
    public IDictionary<string, object> Variables;
    public Type ResultType;
    public CancellationToken CancellationToken;
    public IDictionary<string, string> Headers; 
  }

  public class ServerResponse {
    public IList<RequestError> Errors = new List<RequestError>();
    public object Data;
  }

  public class RequestError {
    public const string ErrorTypeKey = "type";
    public string Message;
    public IList<Location> Locations = new List<Location>();
    public IList<object> Path;
    public IDictionary<string, object> Extensions = new Dictionary<string, object>();

    public RequestError() { }

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

}

