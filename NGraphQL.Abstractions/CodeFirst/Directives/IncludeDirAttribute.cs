using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [DirectiveInfo(
    name: "@include",
    description: "Conditional include directive",
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class IncludeDirAttribute: DirectiveBaseAttribute {
    bool _if;
    public IncludeDirAttribute(bool @if) : base(@if) {
      _if = @if;
    }
  }

}
