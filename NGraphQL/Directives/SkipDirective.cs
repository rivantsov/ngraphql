using System;
using NGraphQL.Introspection;

namespace NGraphQL.Directives {

  public class SkipDirective : IDirectiveInstance {
    bool _if;
    public SkipDirective(bool @if) {
      _if = @if;
      ArgValues = new object[] { _if };
    }

    public object[] ArgValues { get; }
  }

}
