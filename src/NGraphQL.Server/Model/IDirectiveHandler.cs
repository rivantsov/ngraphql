using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model {

  public interface IDirectiveHandler {
    // model directives only
    void ModelDirectiveApply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues);
    void RequestParsed(RuntimeDirective dir);
  }

}
