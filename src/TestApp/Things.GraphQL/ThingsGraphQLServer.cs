using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.Server;

namespace Things.GraphQL {
  public class ThingsGraphQLServer: GraphQLServer {

    public ThingsApp ThingsApp => (ThingsApp)base.App; 

    public ThingsGraphQLServer(ThingsApp app, GraphQLServerSettings settings): base(app, settings) {
      // Register all modules
      this.RegisterModules(new ThingsGraphQLModule());
      // handle specific exceptions if needed and convert them into GraphQL errors. 
      base.Events.OperationError += Events_OperationError;
      base.Initialize(); 
    }

    // Demo of handling special composite exceptions that need to be converted to one or more errors in the response.
    // Ex: VITA ORM defines ClientFaultException that contains multiple validation errors; the call to SaveChanges
    // might throw this exception, with multiple validation errors inside. We will catch the ClientFaultException
    // in the OperationError event handler, unpack errors inside and add them as separate errors in the response. 
    // Here we catch AggregateException to demo/test the technique; we have a resolver that throws this exc with
    // multiple child exceptions inside. 
    private static void Events_OperationError(object sender, OperationErrorEventArgs args) {
      if (args.Exception is AggregateException aex) {
        var ctx = args.RequestContext;
        foreach (var childExc in aex.InnerExceptions) {
          ctx.AddError(childExc.Message, args.RequestItem, errorType: "Aggr-Error");
        }
        // clear original exc
        args.ClearException();
      }
    }


  }
}
