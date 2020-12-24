using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Directives {

  /// <summary> Base class for directives defined as attributes. </summary>
  /// <remarks>
  /// Type system directives (like @deprecated) are defined as attributes that can be placed on .NET artifacts
  /// like classes, members, parameters. The corresponding element in GraphQL schema document will appear with
  /// the directive. 
  /// </remarks>
  public abstract class BaseDirectiveAttribute : Attribute, IDirectiveInstance {
    public string Name { get; }
    public object[] ArgValues { get; }

    public BaseDirectiveAttribute(string name, params object[] argValues) {
      Name = name;
      ArgValues = argValues;
    }
  }
}
