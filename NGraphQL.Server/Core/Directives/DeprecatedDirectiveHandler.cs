using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Core {

  [HandlesDirective("@deprecated")]
  public class DeprecatedDirectiveHandler: DirectiveHandler, IModelDirectiveAction {

    public DeprecatedDirectiveHandler(DirectiveContext context, object[] args) : base(context, args) {
    }

    public void Apply(GraphQLApiModel model, GraphQLModelObject owner) {
      
    }
  }
}
