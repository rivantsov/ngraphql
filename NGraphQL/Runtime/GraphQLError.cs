using System;
using System.Collections.Generic;

namespace NGraphQL.Runtime {

  // see https://spec.graphql.org/June2018/#sec-Errors
  public class GraphQLError {
    public const string ErrorTypeKey = "type";
    public string Message;
    public IList<Location> Locations = new List<Location>();
    public IList<object> Path;
    public IDictionary<string, object> Extensions = new Dictionary<string, object>();

    public GraphQLError() { }

    public GraphQLError(string message, IList<object> path = null, Location location = null, string type = null) {
      Message = message;
      Path = path ?? Array.Empty<object>();
      if (location != null)
        Locations.Add(location);
      if (!string.IsNullOrEmpty(type))
        Extensions[ErrorTypeKey] = type;
    }

    public override string ToString() {
      var str = Message;
      if (Path != null)
        str += " path: [" + string.Join(", ", Path) + "]";
      if (Locations != null && Locations.Count > 0)
        str += $" at: {Locations[0]}";
      return str;
    }
  }

}
