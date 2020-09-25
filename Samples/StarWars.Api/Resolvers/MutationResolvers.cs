using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  public class MutationResolvers : IResolverClass {
    StarWarsApp _app; 
    private Transaction _transaction; 

    // Begin/end request method; mock trans handling
    public void BeginRequest(IRequestContext request) {
      // Get app instance
      var swApi = (StarWarsApi) request.Server.Api;
      _app = swApi.App;
      _transaction = _app.BeginTransaction();  
    }

    public void EndRequest(IRequestContext request) {
      // this is demo, Abort and Commit do nothing
      if (request.Failed)
        _transaction.Abort();
      else
        _transaction.Commit(); 
    }
  }
}
