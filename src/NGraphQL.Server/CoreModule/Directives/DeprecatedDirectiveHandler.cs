using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core {

  // The only thing to do for deprecated dir is to put values into introspection object for schema element. 
  public class DeprecatedDirectiveHandler: IDirectiveHandler {
    
    public void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) {
      var intro = element.Intro_;
      if (intro == null)
        return;
      intro.IsDeprecated = true;
      intro.DeprecationReason = (string) argValues[0]; 
    }

    public void RequestParsed(RuntimeDirective dir) { }

  }

}
