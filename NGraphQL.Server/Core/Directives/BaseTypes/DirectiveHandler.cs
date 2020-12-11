using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  public class DirectiveHandler {
    public DirectiveContext Context;
    public object[] Args;
    public Type[] ActionTypes { get; protected set; }

    public DirectiveHandler (DirectiveContext context, object[] args, params Type[] actionTypes) {
      Context = context;
      Args = args;
      ActionTypes = actionTypes;
    }

  }
}
