using System.Collections.Generic;
using NGraphQL.Introspection;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public abstract class RuntimeDirectiveBase {
    public DirectiveDef Def;
    public DirectiveLocation Location;

    public RuntimeDirectiveBase(DirectiveDef dirDef, DirectiveLocation location) {
      Def = dirDef;
      Location = location; 
    }
    public abstract object[] GetArgValues(RequestContext context);
    public override string ToString() => Def.Name;
  }

  public class RuntimeModelDirective: RuntimeDirectiveBase {
    public ModelDirective Directive;     

    public RuntimeModelDirective(ModelDirective modelDir): base(modelDir.Def, modelDir.Location) {
      Directive = modelDir;
    }

    public override object[] GetArgValues(RequestContext context) {
      return Directive.ModelAttribute.ArgValues;
    }
  }

  public class RuntimeRequestDirective: RuntimeDirectiveBase {
    public RequestDirective Directive; 
    public object[] StaticArgValues;   // dirs that do not use variables
    private static readonly object[] _emptyArgValues = new object[] { };

    public RuntimeRequestDirective(RequestDirective reqDir): base(reqDir.Def, reqDir.Location) {
      Directive = reqDir;
      CheckStaticArgValues(); 
    }
    public override object[] GetArgValues(RequestContext context) {
      return StaticArgValues ?? EvaluateArgs(context); 
    }

    private void CheckStaticArgValues() {
      var mArgs = Directive.MappedArgs;
      if (mArgs == null || mArgs.Count == 0) {
        StaticArgValues = _emptyArgValues;
        return;
      }
      foreach (var marg in mArgs)
        if (!marg.Evaluator.IsConst())
          return; // uses variable, cannot do static args 
      // args do not use vars, so we can evaluate it as static args
      StaticArgValues = EvaluateArgs(null);
    }

    private object[] EvaluateArgs(RequestContext context) {
      var mArgs = Directive.MappedArgs;
      var argValues = new object[mArgs.Count];
      for (int i = 0; i < argValues.Length; i++) {
        argValues[i] = mArgs[i].Evaluator.GetValue(context);
      }
      return argValues;
    }
  }//class 

}
