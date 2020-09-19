using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model.Request {

  /// <summary>Tracks the code path in the request source. Used for reporting path in Error objects
  /// according to GraphQL spec. </summary>
  /// <remarks>Path elements are either field names (aliases) or array indexes. The paths are organized 
  /// as linked lists; each object scope contains a reference to current path element. When error occurs, 
  /// the full path (array of names/indexes) is reconstructed from the linked list. </remarks>
  public class RequestPath {
    public readonly RequestPath Parent;
    public readonly object Value;
    public int FieldDepth; //counts only fields, not indexes; used in limiting depth quota

    // creates root, empty path
    public RequestPath() { }

    private RequestPath(RequestPath parent, object value, int fieldDepth) {
      Parent = parent;
      Value = value;
      FieldDepth = fieldDepth;
    }

    public RequestPath Append(string fieldName) {
      var depth = Parent == null ? 1 : Parent.FieldDepth + 1; 
      return new RequestPath(this, fieldName, depth); 
    }
    public RequestPath Append(int index) {
      var depth = Parent == null ? 0 : Parent.FieldDepth; // we do not increase field depth for indexes
      return new RequestPath(this, index, depth);
    }


    public IList<object> GetFullPath() {
      var path = Parent == null ? new List<object>() : Parent.GetFullPath();
      if (Value != null)
        path.Add(this.Value);
      return path; 
    }

    public override string ToString() {
      return "[" + string.Join(", ", GetFullPath()) + "]";
    }
  }
}
