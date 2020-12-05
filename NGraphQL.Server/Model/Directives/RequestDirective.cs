using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core;

namespace NGraphQL.Model {

  public class Directive {
    public readonly DirectiveContext Context;
    public readonly object[] ArgValues; 

    public Directive(DirectiveContext context, params object[] argValues) {
      Context = context; 
      ArgValues = argValues;
    }

    public static readonly IList<Directive> EmptyList = new Directive [] { }; 
  }

}
