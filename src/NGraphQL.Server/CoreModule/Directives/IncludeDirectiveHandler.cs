using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Core {

  public class IncludeDirectiveHandler: IDirectiveHandler, ISelectionItemDirectiveAction {
    public void AfterResolve(FieldContext context, object[] argValues, ref object value) {
    }

    public void BeforeResolve(FieldContext context, object[] argValues) {
      context.Skip |= !(bool)argValues[0];
    }

    public void PreviewItem(FieldContext context, object[] argValues) {
    }
  }

}
