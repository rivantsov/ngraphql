using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL {

  // see https://spec.graphql.org/June2018/#sec-Errors
  public class GraphQLError {
    private static object[] _emptyPath = new object[] { }; 

    public const string ErrorCodeKey = "code";
    public string Message;
    public IList<SourceLocation> Locations = new List<SourceLocation>();
    public IList<object> Path;
    public IDictionary<string, object> Extensions = new Dictionary<string, object>(); 

    public GraphQLError(string message, IList<object> path = null, SourceLocation location = null, string type = null) {
      Message = message;
      Path = path?.ToArray() ?? _emptyPath;
      if (location != null)
        Locations.Add(location);
      if (!string.IsNullOrEmpty(type))
        Extensions[ErrorCodeKey] = type;
    }

    public override string ToString() {
      var sb = new StringBuilder();
      sb.AppendLine(Message);
      if (Path != null && Path.Count > 0)
        sb.AppendLine($" Path: [ {string.Join(", ", Path)} ]");
      if (Locations != null && Locations.Count > 0)
        sb.AppendLine($"Locations: {string.Join("; ", Locations)}");
      if (Extensions != null && Extensions.Count > 0) {
        sb.AppendLine($"Extensions: [");
        foreach (var kv in Extensions)
          sb.AppendLine($"  {kv.Key} = {kv.Value}");
        sb.AppendLine($"]");
      }
      return sb.ToString();
    }
  }

  public class SourceLocation {
    public int Line;
    public int Column;

    public static readonly SourceLocation StartLocation = new SourceLocation() { Line = 1, Column = 1 };
    public override string ToString() => $"({Line}, {Column})";
  }

}
