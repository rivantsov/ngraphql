using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core;

namespace NGraphQL.Model {

  public class RuntimeDirective {
    public readonly DirectiveContext Context;
    public readonly object[] ArgValues;
    public DirectiveDef Def => Context.Def;
    public string Name => Def.Name;

    public RuntimeDirective(DirectiveContext context, params object[] argValues) {
      Context = context; 
      ArgValues = argValues;
    }

    public static readonly IList<RuntimeDirective> EmptyList = new RuntimeDirective [] { }; 
  }

}
