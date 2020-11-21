using System.Collections.Generic;

namespace NGraphQL.Client {

  public class RequestError {
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

}

