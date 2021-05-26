using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Core {

  public class SkipDirectiveHandler: IDirectiveHandler {
    public void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) { }

    public void RequestParsed(RuntimeDirective dir) {
      var reqDir = dir.Source as RequestDirective;
      if (reqDir == null)
        return;
      var selItem = reqDir.Parent as SelectionItem;
      selItem.Executing += (sender, args) => {
        var argValues = dir.StaticArgValues ?? args.Context.EvaluateArgs(dir.MappedArgs);
        var skip = (bool)argValues[0];
        args.Skip |= skip;
      };
    }

  }
}
