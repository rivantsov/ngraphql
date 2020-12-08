using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  public class DirectiveHandler {
    public DirectiveDef Def;
    public object[] Args; 

    public DirectiveHandler (DirectiveContext context, object[] args) {
      Def = context.Def;
      Args = args; 
    }

  }
}
