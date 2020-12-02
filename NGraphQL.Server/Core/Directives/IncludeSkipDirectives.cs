using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  public class IncludeSkipDirectiveAction: DirectiveAction {
    bool _if;     
    public IncludeSkipDirectiveAction(IDirectiveContext context, bool ifCond, bool isSkip) {
      _if = ifCond;  
    }
    
  }

  public class IncludeSkipResolvers {

    [DefineDirective(
      name: "@include",
      description: "Conditional include directive",
      locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
      listInSchema: false
      )]
    public IncludeSkipDirectiveAction IncludeDirective(IDirectiveContext context, bool @if) {
      return new IncludeSkipDirectiveAction(context, @if, isSkip: false);
    }

    [DefineDirective(
      name: "@skip",
      description: "Conditional skip directive",
      locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
      listInSchema: false
      )]
    public IncludeSkipDirectiveAction SkipDirective(IDirectiveContext context, bool @if) {
      return new IncludeSkipDirectiveAction(context, @if, isSkip: true);
    }


  }
}
