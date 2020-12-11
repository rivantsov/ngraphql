using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [DirectiveInfo(
    name: "@skip",
    description: "Conditional skip directive",
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class SkipDirAttribute : DirectiveBaseAttribute {
    bool _if;
    public SkipDirAttribute(bool @if) : base(@if) {
      _if = @if;
    }
  }

}
