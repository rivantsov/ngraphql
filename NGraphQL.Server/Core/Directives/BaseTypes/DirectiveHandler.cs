using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  public class DirectiveHandler {
    public DirectiveContext Context;
    public object[] Args;

    public DirectiveHandler (DirectiveContext context, object[] args) {
      Context = context;
      Args = args;
    }

  }
}
