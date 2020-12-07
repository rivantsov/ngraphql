using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model {

  public class DirectiveContext {
    public DirectiveDef Def;
    public DirectiveLocation Location; 
  }
}
