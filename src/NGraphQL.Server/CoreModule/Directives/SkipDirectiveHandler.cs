using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Core {

  public class SkipDirectiveHandler: IDirectiveHandler, ISelectionItemDirectiveAction {

    public void AfterResolve(SelectionItemContext context, object[] argValues, ref object value) {
    }

    public void BeforeResolve(SelectionItemContext context, object[] argValues) {
      context.Skip |= (bool)argValues[0];
    }

    public void PreviewItem(SelectionItemContext context, object[] argValues) {
    }
  }

}
