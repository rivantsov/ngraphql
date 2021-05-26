using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Execution {

  public interface IRuntimeDirectiveHandler: IDirectiveHandler {
    void RequestParsed(DirectiveContext context);
  }

}
