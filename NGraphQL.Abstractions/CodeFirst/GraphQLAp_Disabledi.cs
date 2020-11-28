using System;
using System.Collections.Generic;

namespace NGraphQL.CodeFirst {

  public class GraphQLApi {
    public readonly IList<GraphQLModule> Modules = new List<GraphQLModule>();

    public void RegisterModule(GraphQLModule module) {
      Modules.Add(module); 
    }

  }
}
