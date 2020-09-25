using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  public class StarWarsApi: GraphQLApi {
    public StarWarsApiModule MainModule; 

    public StarWarsApi() {
      MainModule = new StarWarsApiModule();
      this.RegisterModule(MainModule); 
    }
  
  }
}
