using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server.Execution;

namespace NGraphQL.Utilities {
  public static class ServerExtensions {

    public static GraphQLApiModel GetModel(this IRequestContext context) {
      var ctx = (RequestContext)context;
      return ctx.ApiModel; 
    }
  }
}
