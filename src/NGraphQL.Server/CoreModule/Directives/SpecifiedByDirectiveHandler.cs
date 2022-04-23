using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core {

  // The only thing to do for deprecated dir is to put values into introspection object for schema element. 
  public class SpecifiedByDirectiveHandler: IDirectiveHandler {
    
    public void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) {
      var type = element.Intro_ as __Type;
      if (type == null || type.Kind != TypeKind.Scalar)
        return;
      type.SpecifiedBy = argValues[0] as string;
    }

    public void RequestParsed(RuntimeDirective dir) {
    }

  }

}
