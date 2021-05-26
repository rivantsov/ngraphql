using NGraphQL.Model;

namespace NGraphQL.Core {

  public class DeprecatedDirectiveHandler: IDirectiveHandler {

    public void Apply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues) {
      var intro = element.Intro_;
      if (intro == null)
        return;
      intro.IsDeprecated = true;
      intro.DeprecationReason = (string) argValues[0]; 
    }
  }

}
