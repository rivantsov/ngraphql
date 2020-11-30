using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [Directive(
    name: "@include",
    description: null,
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class IncludeDirective: Directive {
    bool _if;     
    public IncludeDirective(IDirectiveContext context, bool @if): base(context) {
      _if = @if;  
    }
  }
}
