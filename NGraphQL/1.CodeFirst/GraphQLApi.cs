using System;
using System.Collections.Generic;

using NGraphQL.Model;
using NGraphQL.Model.Core;
using NGraphQL.Model.Introspection;

namespace NGraphQL.CodeFirst {

  public class GraphQLApi {
    public readonly IList<GraphQLModule> Modules = new List<GraphQLModule>();
    public readonly CoreModule CoreModule;
    public readonly IntrospectionModule IntrospectionModule;

    public GraphQLApiModel Model { get; internal set; }

    public GraphQLApi() {
      CoreModule = new CoreModule(this);
      RegisterModule(CoreModule);
      IntrospectionModule = new IntrospectionModule(this);
      RegisterModule(IntrospectionModule); 
    }
    
    public void RegisterModule(GraphQLModule module) {
      Modules.Add(module); 
    }

  }
}
