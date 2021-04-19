using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Execution {

  public class RuntimeDirective {
    public object Source; //ModelDirective or RequestDirective 

    public DirectiveDef Def;
    public DirectiveLocation Location;
    public IList<MappedArg> MappedArgs; //Request directive only

    public object[] StaticArgValues;   // dirs that do not use variables, or model directives

    public RuntimeDirective(RequestDirective reqDir) {
      Source = reqDir;
      Def = reqDir.Def;
      Location = reqDir.Location;
      MappedArgs = reqDir.MappedArgs; 
      StaticArgValues = MappedArgs.TryEvaluateStaticArgValues(); 
    }

    public RuntimeDirective(ModelDirective modelDir) {
      Source = modelDir;
      Def = modelDir.Def;
      Location = modelDir.Location;
      StaticArgValues = _emptyArray; 
    }
    private static object[] _emptyArray = Array.Empty<object>();

  }//class 

}
