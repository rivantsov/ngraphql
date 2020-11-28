using System;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core {

  [DirectiveMetaData(
    name: "skip",
    description: null,
    locations: DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
    listInSchema: false
    )]
  public class SkipDirective: Directive {
    Arg<bool> _ifArg;
    
    public SkipDirective(IDirectiveContext context, Arg<bool> @if): base(context) {
      _ifArg = @if; 
    }

    public override object GetData(IRequestContext context) {
      return IsIncluded(context);
    }

    public bool IsIncluded(IRequestContext context) {
      return !_ifArg.Evaluate(context);
    }

  }

}
