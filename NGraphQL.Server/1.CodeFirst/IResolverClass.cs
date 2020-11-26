using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.CodeFirst {

  public interface IResolverClass {
    void BeginRequest(IRequestContext request);
    void EndRequest(IRequestContext request);
  }

}
