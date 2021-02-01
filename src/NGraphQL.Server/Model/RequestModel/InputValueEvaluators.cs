using System;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Request {

  public abstract class InputValueEvaluator {
    public InputValueDef InputDef; 
    public TypeRef ResultTypeRef; 
    public RequestObjectBase Anchor;
    public IList<RuntimeModelDirective> Directives = _emptyDirs;
    private static readonly IList<RuntimeModelDirective> _emptyDirs = new RuntimeModelDirective[] { };
    
    public InputValueEvaluator(InputValueDef inputDef, TypeRef resultTypeRef, RequestObjectBase anchor) {
      InputDef = inputDef; 
      ResultTypeRef = resultTypeRef; 
      Anchor = anchor;
      // prepare directives
      if (inputDef.Directives != null)
        Directives = inputDef.Directives.Where(d => d.Def.Handler is IInputValueDirectiveAction)
          .Select(d => new RuntimeModelDirective(d)).ToList(); 
    }
    public override string ToString() => InputDef.ToString();

    protected abstract object Evaluate(RequestContext context);
    public abstract bool IsConst();

    public object GetValue(RequestContext context) {
      try {
        var value = Evaluate(context);
        if (Directives.Count > 0)
          value = ApplyDirectives(context, value);
        return value; 
      } catch(InvalidInputException) {
        throw; 
      } catch(Exception ex) {
        throw new InvalidInputException(ex.Message, Anchor, ex);
      }
    }

    private object ApplyDirectives(RequestContext context, object value) {
      object result = value; 
      foreach(var dir in this.Directives) {
        var action = dir.Def.Handler as IInputValueDirectiveAction;
        result = action.ProcessValue(context, dir.GetArgValues(context), result);
      }
      return result; 
    }
  }

  public class ConstInputValue : InputValueEvaluator {
    public object Value;
    public override string ToString() => $"Const:{Value}";

    public override bool IsConst() => true;
    protected override object Evaluate(RequestContext context) => Value;

    public ConstInputValue(InputValueDef inputDef, TypeRef resultTypeRef, RequestObjectBase anchor, object value) 
        : base(inputDef, resultTypeRef, anchor) {
      Value = value;
    }
  }

  public class VariableRefEvaluator : InputValueEvaluator {
    public VariableDef Variable;
    public override bool IsConst() => false;

    public VariableRefEvaluator(InputValueDef inputDef, VariableDef varDecl) : base(inputDef, varDecl.InputDef.TypeRef, varDecl) {
      Variable = varDecl;
    }
    
    protected override object Evaluate(RequestContext context) {
      var opVar = context.OperationVariables.First(v => v.Variable == this.Variable);
      return opVar.Value;
    }
    public override string ToString() => $"${Variable}";
  }

  public class InputListEvaluator : InputValueEvaluator {
    public TypeRef ElemTypeRef; 
    public InputValueEvaluator[] ElemEvaluators;

    public InputListEvaluator(InputValueDef inputDef, TypeRef resultTypeRef, RequestObjectBase anchor,
                 InputValueEvaluator[] elemEvaluators) : base(inputDef, resultTypeRef, anchor) {
      ElemEvaluators = elemEvaluators;
      if (ResultTypeRef.Kind == TypeKind.NonNull)
        ElemTypeRef = ResultTypeRef.Inner.Inner;
      else
        ElemTypeRef = ResultTypeRef.Inner;
    }

    public override bool IsConst() {
      return ElemEvaluators.All(v => v.IsConst());
    }
    protected override object Evaluate(RequestContext context) {
      var values = this.ElemTypeRef.ClrType.CreateTypedArray(ElemEvaluators.Length);
      for(int i = 0; i < ElemEvaluators.Length; i++) {
        var eval = ElemEvaluators[i];
        var value = eval.GetValue(context);
        var convValue = context.ValidateConvert(value, ElemTypeRef, Anchor);
        values[i] = convValue; 
      }
      return values; 
    }

    public override string ToString() => $"List({ElemEvaluators.Length})";
  }

  public class FlagSetInputEvaluator : InputValueEvaluator {
    public readonly EnumTypeDef EnumTypeDef;
    public InputValueEvaluator[] ElemEvaluators;

    public FlagSetInputEvaluator(InputValueDef inputDef, TypeRef resultTypeRef, RequestObjectBase anchor, 
                    InputValueEvaluator[] valueEvals)
            : base(inputDef, resultTypeRef, anchor) {
      ElemEvaluators = valueEvals;
      EnumTypeDef = (EnumTypeDef) ResultTypeRef.TypeDef;
    }

    protected override object Evaluate(RequestContext context) {
      var values = new object[ElemEvaluators.Length];
      for (int i = 0; i < ElemEvaluators.Length; i++) {
        var eval = ElemEvaluators[i]; 
        var value = eval.GetValue(context);
        values[i] = value; 
      }
      var result = EnumTypeDef.ConvertFlagListToEnumValue(values);
      return result;
    }

    public override bool IsConst() {
      return ElemEvaluators.All(v => v.IsConst());
    }
  }

  public class InputFieldEvalInfo {
    public InputValueDef FieldDef;
    public InputValueEvaluator ValueEvaluator;
    public override string ToString() => $"{FieldDef}={ValueEvaluator}";
  }

  public class InputObjectEvaluator : InputValueEvaluator {
    public IList<InputFieldEvalInfo> Fields = new List<InputFieldEvalInfo>();

    public InputObjectEvaluator (InputValueDef inputDef, TypeRef resultTypeRef, RequestObjectBase anchor,
        IList<InputFieldEvalInfo> fields)  : base(inputDef, resultTypeRef, anchor) {
      Fields = fields;
    }

    public override bool IsConst() {
      return Fields.All(f => f.ValueEvaluator.IsConst());
    }

    protected override object Evaluate(RequestContext context) {
      var obj = Activator.CreateInstance(this.ResultTypeRef.TypeDef.ClrType);
      foreach (var fld in Fields) {
        var value = fld.ValueEvaluator.GetValue(context);
        var convValue = context.ValidateConvert(value, fld.FieldDef.TypeRef, Anchor);
        fld.FieldDef.InputObjectClrMember.SetMember(obj, convValue);
      }
      return obj;
    }
  }

}
