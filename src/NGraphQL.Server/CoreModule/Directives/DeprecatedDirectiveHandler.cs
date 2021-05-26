using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core {

  public class DeprecatedDirectiveHandler: IDirectiveHandler {

    public void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) {
      var intro = element.Intro_;
      if (intro == null)
        return;
      intro.IsDeprecated = true;
      intro.DeprecationReason = (string) argValues[0]; 
    }

    public void RequestParsed(DirectiveContext context) { }

  }

}
