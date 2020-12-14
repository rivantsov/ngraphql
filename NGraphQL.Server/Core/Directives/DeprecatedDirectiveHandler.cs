using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Core {

  [HandlesDirective("@deprecated")]
  public class DeprecatedDirectiveHandler: DirectiveHandler, IModelDirectiveAction {
    public readonly string Reason; 

    public DeprecatedDirectiveHandler(DirectiveContext context, object[] args) : base(context, args) {
      Reason = (string) args[0];
    }

    public void Apply(GraphQLApiModel model, GraphQLModelObject owner) {
      var intro = owner.Intro_;
      if (intro == null)
        return;
      intro.IsDeprecated = true;
      intro.DeprecationReason = Reason; 
    }
  }

}
