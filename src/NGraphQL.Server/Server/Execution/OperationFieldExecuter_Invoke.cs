using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  partial class OperationFieldExecuter {

    private async Task<object> InvokeResolverAsync(SelectionItemContext fieldContext) {
      try {
        if(fieldContext.ResolverClassInstance == null)
          AssignResolverClassInstance(fieldContext);
        if(fieldContext.ArgValues == null)
          BuildResolverArguments(fieldContext);
        // we might have encountered errors when evaluating args; if so, abort all
        this.AbortIfFailed();
        var fldDef = fieldContext.FieldDef;
        // set current parentEntity arg
        if (fldDef.Flags.IsSet(FieldFlags.HasParentArg))
          fieldContext.ArgValues[1] = fieldContext.CurrentScope.Entity;
        var fldResolver = fieldContext.MappedField.Resolver;  
        var clrMethod = fldResolver.ResolverMethod.Method;
        var result = clrMethod.Invoke(fieldContext.ResolverClassInstance, fieldContext.ArgValues);
        if(fldDef.Flags.IsSet(FieldFlags.ResolverReturnsTask))
          result = await UnwrapTaskResultAsync(fieldContext, (Task)result);
        Interlocked.Increment(ref _requestContext.Metrics.ResolverCallCount);
        // Note: result might be null, but batched result might be set.
        return result;
      } catch(TargetInvocationException tex) {
        // sync call goes here
        var origExc = tex.InnerException;
        if (origExc is AbortRequestException)
          throw origExc; 
        fieldContext.AddError(origExc, ErrorCodes.ResolverError);
        Fail();
        return null; //never happens
      } catch(AbortRequestException) {
        throw;
      } catch(Exception ex) {
        fieldContext.AddError(ex, ErrorCodes.ResolverError);
        Fail();
        return null; //never happens
      }
    }

    private async Task<object> UnwrapTaskResultAsync(SelectionItemContext fieldContext, Task task) {
      if (!task.IsCompleted)
        await task; 
      switch(task.Status) {
        
        case TaskStatus.Faulted:
          Exception origExc = task.Exception;
          if(origExc is AggregateException aex)
            origExc = aex.InnerException; //we expect just one exc (we ignore exc.InnerExceptions list)
          fieldContext.AddError(origExc, ErrorCodes.ResolverError);
          Fail();
          return null; 
        
        case TaskStatus.RanToCompletion:
          var resReader = fieldContext.MappedField.Resolver.ResolverMethod.TaskResultReader;
          var result = resReader(task);
          return result;

        case TaskStatus.Canceled:
        default:
          var msg = "Resolver execution canceled.";
          fieldContext.AddError(msg, ErrorCodes.Cancelled);
          throw new ResolverException(msg); 
      }
    }

    private object InvokeFieldReader(SelectionItemContext fieldContext, object parent) {
      try {
        var reader = fieldContext.MappedField.Resolver.ResolverFunc;
        var result = reader(parent);
        return result;
      } catch(TargetInvocationException tex) {
        // sync call goes here
        var origExc = tex.InnerException ?? tex;
        fieldContext.AddError(origExc, ErrorCodes.ResolverError);
        throw new AbortRequestException();
      } 
    }

    private void BuildResolverArguments(SelectionItemContext fieldContext) {
      // arguments
      var field = fieldContext.MappedField;
      var argValues = new List<object>();
      // special arguments: context, parent      
      argValues.Add(fieldContext);
      if(field.FieldDef.Flags.IsSet(FieldFlags.HasParentArg))
        argValues.Add(fieldContext.CurrentScope.Entity); 
      //regular arguments
      for(int i = 0; i < field.Args.Count; i++) {
        var arg = field.Args[i];
        var argValue = SafeEvaluateArg(fieldContext, arg);
        argValues.Add(argValue);
      }
      fieldContext.ArgValues = argValues.ToArray();
    }

    private object SafeEvaluateArg(SelectionItemContext fieldContext, MappedArg arg) {
      try {
        var value = arg.Evaluator.GetValue(_requestContext);
        var convValue = _requestContext.ValidateConvert(value, arg.ArgDef.TypeRef, arg.Anchor);
        return convValue; 
      } catch (AbortRequestException) {
        return null;
      } catch(InvalidInputException bvEx) {
        _requestContext.AddInputError(bvEx);
        _failed = true;
        return null; //continue evaluating args; it will be aborted after all args are done 
      } catch(Exception ex) {
        _requestContext.AddInputError($"Failed to evaluate argument {arg.ArgDef.Name}: {ex.Message}", arg.Anchor);
        _failed = true;
        return null; // continue to next arg
      }
    }

    // gets cached resolver class instance or creates new one
    private void AssignResolverClassInstance(SelectionItemContext fieldCtx) {
      var resType = fieldCtx.MappedField.Resolver.ResolverMethod.ResolverClass.Type;
      object resolver = null; 
      if (_resolverInstances.Count == 1 && _resolverInstances[0].GetType() == resType) // fast track
        resolver = _resolverInstances[0];
      else 
        resolver = _resolverInstances.FirstOrDefault(r => r.GetType() == resType);
      if(resolver == null) {
        resolver = Activator.CreateInstance(resType);
        if(resolver is IResolverClass iRes)
          iRes.BeginRequest(_requestContext);
        _resolverInstances.Add(resolver); 
      }
      fieldCtx.ResolverClassInstance = resolver;
    }

  } //class
}
