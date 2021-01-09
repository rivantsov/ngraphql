using System;
using Irony.Parsing;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public class DirectiveContext {
    public DirectiveDef Def;
    public DirectiveLocation Location;
    public object Owner;
    public Irony.Parsing.SourceLocation SourceLocation;
  }
}
