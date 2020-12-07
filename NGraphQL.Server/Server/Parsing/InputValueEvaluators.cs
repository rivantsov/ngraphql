using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Parsing {

  public abstract class InputValueEvaluator: IInputValueEvaluator {
    public IValueTarget Target; 
    public TypeRef ResultTypeRef; 
    public RequestObjectBase Anchor;
    public InputValueEvaluator(IValueTarget target, TypeRef resultTypeRef, RequestObjectBase anchor) {
      Target = target; 
      ResultTypeRef = resultTypeRef; 
      Anchor = anchor; 
    }

    protected abstract object Evaluate(RequestContext context);
    public abstract bool IsConst();

    public object GetValue(RequestContext context) {
      try {
        var value = Evaluate(context);
        value = this.Target.Directives.ApplyDirectives<IArgDirectiveAction>((d, v) => d.CheckArgValue(context, v), value);
        return value; 
      } catch(InvalidInputException) {
        throw; 
      } catch(Exception ex) {
        throw new InvalidInputException(ex.Message, Anchor, ex);
      }
    }
  }

  public class ConstInputValue : InputValueEvaluator {
    public object Value;
    public override string ToString() => $"Const:{Value}";

    public override bool IsConst() => true;
    protected override object Evaluate(RequestContext context) => Value;

    public ConstInputValue(IValueTarget target, TypeRef resultTypeRef, RequestObjectBase anchor, object value) 
        : base(target, resultTypeRef, anchor) {
      Value = value;
    }
  }

  public class VariableRefEvaluator : InputValueEvaluator {
    public VariableDef Variable;
    public override bool IsConst() => false;

    public VariableRefEvaluator(IValueTarget target, VariableDef varDecl) : base(target, varDecl.TypeRef, varDecl) {
      Variable = varDecl;
    }
    
    protected override object Evaluate(RequestContext context) {
      var opVar = context.OperationVariables.First(v => v.Variable == this.Variable);
      return opVar.Value;
    }
    public override string ToString() => $"Variable";
  }

  public class InputListEvaluator : InputValueEvaluator {
    public TypeRef ElemTypeRef; 
    public InputValueEvaluator[] ElemEvaluators;

    public InputListEvaluator(IValueTarget target, TypeRef resultTypeRef, RequestObjectBase anchor,
                 InputValueEvaluator[] elemEvaluators) : base(target, resultTypeRef, anchor) {
      ElemEvaluators = elemEvaluators;
      if (ResultTypeRef.Kind == TypeKind.NotNull)
        ElemTypeRef = ResultTypeRef.Parent.Parent;
      else
        ElemTypeRef = ResultTypeRef.Parent;
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
    private TypeRef _stringTypeRef; 

    public FlagSetInputEvaluator(IValueTarget target, TypeRef resultTypeRef, RequestObjectBase anchor, 
                    InputValueEvaluator[] valueEvals)
            : base(target, resultTypeRef, anchor) {
      ElemEvaluators = valueEvals;
      EnumTypeDef = (EnumTypeDef) ResultTypeRef.TypeDef;
    }

    protected override object Evaluate(RequestContext context) {
      _stringTypeRef = _stringTypeRef ?? context.Server.CoreModule.String_.TypeRefNotNull;
      var values = new object[ElemEvaluators.Length];
      for (int i = 0; i < ElemEvaluators.Length; i++) {
        var eval = ElemEvaluators[i]; 
        var value = eval.GetValue(context);
        var convValue = context.ValidateConvert(value, _stringTypeRef, Anchor); 
        values[i] = convValue; 
      }
      var result = EnumTypeDef.CombineFlags(values);
      return result;
    }
    public override string ToString() => $"Enum({EnumTypeDef.Name})";

    public override bool IsConst() {
      return ElemEvaluators.All(v => v.IsConst());
    }
  }

  public class InputFieldEvalInfo {
    public InputValueDef FieldDef;
    public InputValueEvaluator ValueEvaluator; 
  }

  public class InputObjectEvaluator : InputValueEvaluator {
    public IList<InputFieldEvalInfo> Fields = new List<InputFieldEvalInfo>();

    public InputObjectEvaluator (IValueTarget target, TypeRef resultTypeRef, RequestObjectBase anchor,
        IList<InputFieldEvalInfo> fields)  : base(target, resultTypeRef, anchor) {
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
