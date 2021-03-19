using System;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Parsing {

  partial class RequestMapper {

    private IList<MappedSelectionFieldArg> MapArguments(IList<InputValue> args, IList<InputValueDef> argDefs, NamedRequestObject owner) {
      args ??= InputValue.EmptyList; 
      var hasArgs = args.Count > 0; 
      var hasArgDefs = argDefs.Count > 0; // argDefs are never null
      // some corner cases first
      if(!hasArgDefs && !hasArgs)
        return MappedSelectionFieldArg.EmptyList;
      if(!hasArgDefs && hasArgs) {
        AddError($"No arguments are expected for {owner.Name}", owner);
        return MappedSelectionFieldArg.EmptyList;
      }

      // we have args and argDefs; first check that arg names are valid
      var unknownArgs = args.Where(a => !argDefs.Any(ad => ad.Name == a.Name)).ToList();
      if (unknownArgs.Count > 0) { 
        foreach(var ua in unknownArgs) {
          AddError($"Field(dir) {owner.Name}: argument {ua.Name} not defined.", ua);
        }
        return MappedSelectionFieldArg.EmptyList; 
      }

      // build Mapped Arg list - full list of args in right order matching the resolver method
      var mappedArgs = new List<MappedSelectionFieldArg>();
      foreach(var argDef in argDefs) {
        var arg = args.FirstOrDefault(a => a.Name == argDef.Name);
        if(arg == null) {
          // nullable args have default value null
          if(argDef.HasDefaultValue || !argDef.TypeRef.IsNotNull) {
            var constValue = CreateConstantInputValue(argDef, owner, argDef.TypeRef, argDef.DefaultValue); 
            var mappedArg = new MappedSelectionFieldArg() { Anchor = owner, ArgDef = argDef, Evaluator = constValue };
            mappedArgs.Add(mappedArg);
          } else {
            AddError($"Field(dir) '{owner.Name}': argument '{argDef.Name}' value is missing.", owner);
          }
          continue;
        }
        // arg != null
        try {
          var argEval = GetInputValueEvaluator(argDef, arg.ValueSource, argDef.TypeRef);
          var outArg = new MappedSelectionFieldArg() { Anchor = arg, ArgDef = argDef, Evaluator = argEval };
          mappedArgs.Add(outArg);
        } catch (InvalidInputException bvEx) {
          _requestContext.AddInputError(bvEx);
          continue;
        } catch (Exception ex) {
          throw new InvalidInputException(ex.Message, arg.ValueSource, ex);
        }
      } //foreach argDef
      return mappedArgs;
    }

    internal InputValueEvaluator GetInputValueEvaluator(InputValueDef inputDef, ValueSource valueSource, TypeRef valueTypeRef) {
      if (valueSource.IsConstNull())
        return CreateConstantInputValue(inputDef, valueSource, valueTypeRef, null);
      var eval = GetInputValueEvaluatorImpl(inputDef, valueSource, valueTypeRef);
      // replace with constant if it does not depend on vars
      if (eval.IsConst()) {
        var value = eval.GetValue(_requestContext);
        eval = CreateConstantInputValue(inputDef, valueSource, valueTypeRef, value);
      }
      return eval;
    }

    private ConstInputValue CreateConstantInputValue(InputValueDef inputDef, RequestObjectBase anchor, TypeRef resultTypeRef, object value) {
      // We convert const value upfront to target typeRef
      var  convValue = _requestContext.ValidateConvert(value, resultTypeRef, anchor);
      var constEval = new ConstInputValue(inputDef, resultTypeRef, anchor, convValue);
      return constEval; 
    }

    private InputValueEvaluator GetInputValueEvaluatorImpl(InputValueDef inputDef, ValueSource valueSource, TypeRef resultTypeRef) {
      if(valueSource is VariableValueSource vref)
        return GetVariableRefEvaluator(inputDef, resultTypeRef, vref);
      if (resultTypeRef.IsList)
        return GetInputListEvaluator(inputDef, valueSource, resultTypeRef);
      
      switch(resultTypeRef.TypeDef) {
        case ScalarTypeDef stdef:
          if(valueSource is TokenValueSource tknIv) {
            var constValue = stdef.Scalar.ParseToken(_requestContext, tknIv.TokenData);
            return CreateConstantInputValue(inputDef, valueSource, resultTypeRef, constValue); 
          } else {
            throw new InvalidInputException("invalid input value, expected scalar", valueSource); 
          }
        
        case EnumTypeDef etdef:
          var handler = etdef.Handler;
          if(handler.IsFlagSet && valueSource is ListValueSource)
            return GetInputListEvaluator(inputDef, valueSource, resultTypeRef);
          if(valueSource is TokenValueSource tknValueSrc) { 
            if(tknValueSrc.TokenData.TermName != TermNames.Name)
              throw new InvalidInputException($"Invalid value '{tknValueSrc.TokenData.Text}', expected Enum value.", valueSource);
            var vText = tknValueSrc.TokenData.Text;
            var enumVal = handler.ConvertStringToEnumValue(vText);
            return CreateConstantInputValue(inputDef, tknValueSrc, resultTypeRef, enumVal);
          } else {
            throw new InvalidInputException($"Invalid input value, expected enum value.", valueSource); 
          }

        case InputObjectTypeDef inpObjDef:
          return GetInputObjectEvaluator(inputDef, valueSource, resultTypeRef);

        default:
          return null; //never happens 
      }
    }

    private VariableRefEvaluator GetVariableRefEvaluator(InputValueDef inputDef, TypeRef resultTypeRef, VariableValueSource varRef) {
      var varDecl = _currentOp.Variables.FirstOrDefault(vd => vd.Name == varRef.VariableName);
      if (varDecl == null)
        throw new InvalidInputException($"Variable {varRef.VariableName} not defined.", varRef);
      // check type compatibility 
      if(!resultTypeRef.IsConvertibleFrom(varDecl.InputDef.TypeRef)) 
        throw new InvalidInputException(
          $"Incompatible types: variable ${varRef.VariableName} cannot be converted to type '{resultTypeRef.Name}'", varRef);
      return new VariableRefEvaluator(inputDef, varDecl);
    }

    private InputObjectEvaluator GetInputObjectEvaluator(InputValueDef inputDef, ValueSource valueSource, TypeRef typeRef) {
      var inpObjTypeDef = (InputObjectTypeDef)typeRef.TypeDef;
      // valueSource is not null (its value), we already checked it before coming here
      if (!(valueSource is ObjectValueSource parsedInputObj))
        throw new InvalidInputException($"Value is not InputObject, expected value of type '{typeRef.Name}'.", valueSource);
      var fields = new List<InputFieldEvalInfo>();
      foreach(var fldDef in inpObjTypeDef.Fields) {
        InputValueEvaluator fldEval;
        if (parsedInputObj.Fields.TryGetValue(fldDef.Name, out var inpValue))
          fldEval = GetInputValueEvaluator(fldDef, inpValue, fldDef.TypeRef);
        else if (fldDef.HasDefaultValue)
          fldEval = CreateConstantInputValue(fldDef, valueSource, fldDef.TypeRef, fldDef.DefaultValue);
        else if (!fldDef.TypeRef.IsNotNull)
          fldEval = CreateConstantInputValue(fldDef, valueSource, fldDef.TypeRef, null);
        else {
          throw new InvalidInputException($"Missing value for field '{fldDef.Name}'.", valueSource);
        }
        fields.Add(new InputFieldEvalInfo() { FieldDef = fldDef, ValueEvaluator = fldEval });
        // TODO: add check that there are no 'extra' members in parsed object
      }
      var result = new InputObjectEvaluator(inputDef, typeRef, valueSource, fields);
      return result;
    }

    private InputValueEvaluator GetInputListEvaluator(InputValueDef inputDef, ValueSource valueSource, TypeRef listTypeRef) {
      if(valueSource.IsConstNull()) {
        if (listTypeRef.IsNotNull)
          throw new InvalidInputException("Input type '{valueTypeRef.Name}' is not-null type, but null was encountered.", 
            valueSource);
      }
      if (!(valueSource is ListValueSource arrValue))
        throw new InvalidInputException($"Input type '{listTypeRef.Name}' is not list or array, expected array.", valueSource);
      var elemTypeRef = listTypeRef.GetListElementTypeRef();
      if (elemTypeRef == null) 
        throw new InvalidInputException($"Invalid input value for type {listTypeRef.Name}, expected list", valueSource);
      var elemEvalList =  arrValue.Values.Select(v => GetInputValueEvaluator(inputDef, v, elemTypeRef)).ToArray();
      // it can be regular array or enum flag set (which is special)
      if (elemTypeRef.TypeDef.IsEnumFlagArray()) {
        return new FlagSetInputEvaluator(inputDef, listTypeRef, valueSource, elemEvalList);
      } else {
        return new InputListEvaluator(inputDef, listTypeRef, valueSource, elemEvalList);
      }
    }

  }
}
