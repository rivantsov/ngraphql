using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  /****************************************************************************************
   A GraphQL API on top of a sample 'business' app
  ****************************************************************************************/

  // Custom GraphQLApi class - a container for Api modules
  // an instance of Api class is provided to GraphQLServer which hosts/runs the API
  public class ThingsApi : GraphQLApi {
    public readonly ThingsApp App;
    
    public ThingsApi(ThingsApp thingsApp) {
      App = thingsApp; 
      var thingsModule = new ThingsGraphQLModule(this);
      base.RegisterModule(thingsModule);
    }
  }

}
