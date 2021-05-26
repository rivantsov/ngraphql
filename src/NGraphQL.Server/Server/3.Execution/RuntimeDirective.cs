using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Execution {

  // RuntimeDirective is representation of a @dir in request. It is attached to parsed request elements
  //  Note that parsed request might be cached and reused/re-executed with different params.
  //  For each request execution we create DirectiveContext for each directive in the request (full list in RequestContext.DirectiveContexts)
  public class RuntimeDirective {
    public int Index; // index to lookup DirectiveContext in requestContext
    public object Source; //ModelDirective or RequestDirective 
    public object Owner; //  SelectionItem or MappedArg 
    public DirectiveDef Def;
    public DirectiveLocation Location;

    public IList<MappedArg> MappedArgs;
    public object[] StaticArgValues;   // dirs that do not use variables, or model directives

    public RuntimeDirective(RequestDirective reqDir, int index) {
      Source = reqDir;
      Index = index; 
      Def = reqDir.Def;
      Location = reqDir.Location;
      MappedArgs = reqDir.MappedArgs;
      StaticArgValues = reqDir.MappedArgs.TryEvaluateStaticArgValues(); 
    }

    public RuntimeDirective(ModelDirective modelDir, int index) {
      Source = modelDir;
      Index = index; 
      Def = modelDir.Def;
      Location = modelDir.Location;
      StaticArgValues = modelDir.ArgValues; 
    }
    private static object[] _emptyArray = Array.Empty<object>();

  }//class 

}
