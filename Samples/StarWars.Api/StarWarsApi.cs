using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  public class StarWarsApi: GraphQLApi {
    public readonly StarWarsApp App; 
    public readonly StarWarsApiModule MainModule; 

    public StarWarsApi(StarWarsApp app) {
      App = app; 
      MainModule = new StarWarsApiModule(this);
      this.RegisterModule(MainModule); 
    }
  
  }
}
