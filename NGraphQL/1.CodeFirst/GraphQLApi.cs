using System;
using System.Collections.Generic;

using NGraphQL.Model;
using NGraphQL.Model.Core;
using NGraphQL.Model.Introspection;

namespace NGraphQL.CodeFirst {

  public class GraphQLApi {
    public readonly IList<GraphQLModule> Modules = new List<GraphQLModule>();
    public readonly CoreModule CoreModule;
    public readonly IntrospectionModule IntrospectionModeule;
    public readonly List<ScalarTypeDef> Scalars = new List<ScalarTypeDef>();
    public readonly List<DirectiveDef> Directives = new List<DirectiveDef>();


    public GraphQLApiModel Model { get; internal set; }

    public GraphQLApi() {
      CoreModule = new CoreModule(this);
      RegisterModule(CoreModule);
      IntrospectionModeule = new IntrospectionModule();
      RegisterModule(IntrospectionModeule); 
    }
    
    public void RegisterModule(GraphQLModule module) {
      Modules.Add(module); 
    }

  }
}
