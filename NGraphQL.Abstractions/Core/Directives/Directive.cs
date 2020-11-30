using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  public class Directive {
    public IDirectiveInfo Info; 

    public Directive(IDirectiveContext context) {
      Info = context.DirectiveInfo; 
    }

    public virtual object ApplyInput(IRequestContext context, object value) => value; 
    public virtual object ApplyOutput(IRequestContext context, object value) => value;
  }
}
