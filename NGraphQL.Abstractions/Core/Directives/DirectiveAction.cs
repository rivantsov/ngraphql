using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  public class DirectiveAction {
    public IDirectiveInfo Directive { get; }   
    
    public DirectiveAction(IDirectiveInfo directiveInfo) {
      Directive = directiveInfo; 
    }

    public virtual object ApplyInput(IRequestContext context, object value) => value; 
    public virtual object ApplyOutput(IRequestContext context, object value) => value;
  }
}
