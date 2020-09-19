using System;
using System.Collections.Generic;

using NGraphQL.Model;
using NGraphQL.Model.Core;
using NGraphQL.Model.Introspection;

namespace NGraphQL.CodeFirst {

  public class GraphQLApi {
    public readonly IList<GraphQLModule> Modules = new List<GraphQLModule>();
    public readonly CoreModule CoreTypes;
    public readonly IntrospectionModule IntrospectionTypes; 

    public GraphQLApiModel Model { get; internal set; }

    public GraphQLApi() {
      CoreTypes = new CoreModule(this);
      RegisterModule(CoreTypes);
      IntrospectionTypes = new IntrospectionModule();
      RegisterModule(IntrospectionTypes); 
    }
    
    public void RegisterModule(GraphQLModule module) {
      Modules.Add(module); 
    }

  }
}
