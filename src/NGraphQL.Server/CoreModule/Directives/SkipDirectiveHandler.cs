using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Core {

  public class SkipDirectiveHandler: IRuntimeDirectiveHandler {

    public void RequestParsed(DirectiveContext context) {
      var selItem = context.Directive.Owner as SelectionItem;
      var skip = (bool)context.ArgValues[0];
      selItem.Executing += (sender, args) => {
        args.Skip |= skip;
      };
    }


    public void AfterResolve(FieldContext context, object[] argValues, ref object value) {
    }

    public void BeforeResolve(FieldContext context, object[] argValues) {
      context.Skip |= (bool)argValues[0];
    }

    public void PreviewItem(FieldContext context, object[] argValues) {
    }
  }

}
