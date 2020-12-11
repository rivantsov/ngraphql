using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [DirectiveInfo(
    name: "@skip",
    description: "Conditional skip directive",
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class SkipDirectiveAttribute : DirectiveBaseAttribute {
    bool _if;
    public SkipDirectiveAttribute(bool @if) : base(@if) {
      _if = @if;
    }
  }

}
