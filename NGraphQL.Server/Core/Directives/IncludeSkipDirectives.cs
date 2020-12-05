using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Core {

  [DirectiveInfo(
    name: "@include",
    description: "Conditional include directive",
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class IncludeDirective: Directive, ISkipFieldDirectiveAction {
    bool _if;
    public IncludeDirective(DirectiveContext context, bool @if) : base(context, @if) {
      _if = @if;
    }

    public bool SkipField(RequestContext context, MappedField field) => !_if; 
  }

  [DirectiveInfo(
    name: "@skip",
    description: "Conditional skip directive",
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class SkipDirective : Directive, ISkipFieldDirectiveAction {
    bool _if;
    public SkipDirective(DirectiveContext context, bool @if) : base(context, @if) {
      _if = @if;
    }

    public bool SkipField(RequestContext context, MappedField field) => _if;
  }

}
