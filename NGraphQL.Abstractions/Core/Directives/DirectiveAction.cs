using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  public class DirectiveAction {
    public static readonly DirectiveAction EmptyAction = new DirectiveAction(); 

    public virtual void PreviewField(IFieldContext context) { }
    public virtual object ApplyToOutput(IRequestContext context, object value) => value;
    public virtual object PreviewArg(IRequestContext context, object value) => value; 
  }
}
