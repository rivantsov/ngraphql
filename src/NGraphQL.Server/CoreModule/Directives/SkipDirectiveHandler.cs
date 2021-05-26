using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Core {

  public class SkipDirectiveHandler: IDirectiveHandler {
    public void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) { }

    public void RequestParsed(DirectiveContext context) {
      var selItem = context.Directive.Owner as SelectionItem;
      var skip = (bool)context.ArgValues[0];
      selItem.Executing += (sender, args) => {
        args.Skip |= skip;
      };
    }

  }
}
