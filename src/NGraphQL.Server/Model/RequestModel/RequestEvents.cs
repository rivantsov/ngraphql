using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public class SelectionItemExecutingEventArgs: EventArgs {
    public RequestContext Context; 
    public bool Skip;  

    public SelectionItemExecutingEventArgs(RequestContext context) {
      Context = context; 
    }
  }

}
