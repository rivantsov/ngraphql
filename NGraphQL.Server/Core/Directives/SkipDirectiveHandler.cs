using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Core {

  [HandlesDirective("@skip")]
  public class SkipDirectiveHandler: DirectiveHandler, ISkipDirectiveAction {
    bool _if; 

    public SkipDirectiveHandler(DirectiveContext context, object[] args) 
      : base(context, args, typeof(ISkipDirectiveAction)) {
      _if = (bool)args[0];
    }

    public bool ShouldSkip(RequestContext context, MappedField field) => _if; 
  }

}
