using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [Directive(
    name: "@skip",
    description: null,
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class SkipDirective : Directive {
    bool _if;
    public SkipDirective(IDirectiveContext context, bool @if) : base(context) {
      _if = @if;
    }

  }
}
