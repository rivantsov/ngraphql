using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor;
    public InputValueDef ArgDef;
    public InputValueEvaluator Evaluator;
    public List<RuntimeDirective> Directives;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

}
