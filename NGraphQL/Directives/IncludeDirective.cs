using System.Diagnostics;
using NGraphQL.Introspection;

namespace NGraphQL.Directives {

  public class IncludeDirective: IDirectiveInstance {
    bool _if;
    public IncludeDirective(bool @if) {
      _if = @if;
      ArgValues = new object[] { _if };
    }

    public object[] ArgValues { get; }
  }

}
