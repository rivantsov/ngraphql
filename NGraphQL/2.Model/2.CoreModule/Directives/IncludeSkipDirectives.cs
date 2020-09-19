using System; 
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Core {

  public interface IIncludeSkipDirectiveDef {
    bool IsIncluded(RequestDirective dir, RequestContext requestContext);
  }

  public abstract class IncludeSkipDirectiveDefBase: DirectiveDef, IIncludeSkipDirectiveDef {
    private InputValueDef _ifArgDef;
    private bool _isSkip;
    
    public IncludeSkipDirectiveDefBase(CoreModule sys, string name, bool isSkip) {
      base.Name = name;
      _isSkip = isSkip; 
      base.Locations = DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment;
      _ifArgDef = new InputValueDef() { Name = "if", TypeRef = sys.Boolean_.TypeRefNotNull };
      base.Args = new InputValueDef[] { _ifArgDef };
    }

    public bool IsIncluded(RequestDirective dir, RequestContext requestContext) {
      var mArg = dir.MappedArgs[0];
      var result = mArg.Evaluator.GetValue(requestContext);
      var bRes = (bool)result; 
      if(_isSkip)
        result = !bRes;
      return bRes; 
    }
  }

  public class IncludeDirectiveDef: IncludeSkipDirectiveDefBase {
    public IncludeDirectiveDef(CoreModule sys) : base(sys, "@include", false) { }
  }
  public class SkipDirectiveDef : IncludeSkipDirectiveDefBase {
    public SkipDirectiveDef(CoreModule sys) : base(sys, "@skip", true) { }
  }
}
