using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  [DirectiveMetaData(
    name: "include",
    description: null,
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class IncludeDirective: Directive {
    Arg<bool> _ifArg;
    
    public IncludeDirective(IDirectiveContext context, Arg<bool> @if): base(context) {
      _ifArg = @if; 
    }

    public override object GetData(IRequestContext context) {
      return IsIncluded(context);
    }

    public bool IsIncluded(IRequestContext context) {
      return _ifArg.Evaluate(context);
    }

  }

}
