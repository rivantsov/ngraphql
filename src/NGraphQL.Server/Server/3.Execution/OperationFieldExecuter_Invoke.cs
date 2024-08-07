﻿using System;
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

    private async Task<object> InvokeResolverAsync(FieldContext fieldContext) {
      try {
        // Invoke resolver
        object result;
        var fldParScope = fieldContext.CurrentParentScope;
        if (fldParScope.IsSubscriptionNextTopScope) // special case for Subscription
          result = fldParScope.SubscriptionNextResolverResult;
        else if (fieldContext.MappedField.Resolver.ResolverFunc != null)
          result = InvokeResolverFunc(fieldContext);
        else
          result = await InvokeResolverMethodAsync(fieldContext);
        // if it was batch result set, and result is null, lookup result from current scope
        //  we still need to return real result 
        object convResult;
        if (result == null && fieldContext.BatchResultWasSet) {
          var fldKey = fieldContext.MappedField.Field.Key;
          fieldContext.CurrentParentScope.TryGetValue(fldKey, out convResult); // value already converted
        } else {
          convResult = fieldContext.ConvertToOutputValue(result);
        }
        // check for null in non-null field; this check is suppressed by IgnoreOutNullFaults flag in server options
        if (!_ignoreOutNullFaults && convResult == null && fieldContext.FieldDef.TypeRef.IsNotNull ) {
          var selFld = fieldContext.MappedField.Field;
          _requestContext.AddError($"Server error: resolver for non-nullable field '{selFld.Key}' returned null.",
                selFld, ErrorCodes.ServerError);
        }
        return convResult;
      } catch (TargetInvocationException tex) {
        // sync call goes here
        var origExc = tex.InnerException;
        if (origExc is AbortRequestException)
          throw origExc;
        AddError(fieldContext, origExc, ErrorCodes.ResolverError);
        Fail(); // throws
        return null; //never happens
      } catch (AbortRequestException) {
        throw;
      } catch (Exception ex) {
        AddError(fieldContext, ex, ErrorCodes.ResolverError);
        Fail();
        return null; //never happens
      }
    }

    private async Task<object> InvokeResolverMethodAsync(FieldContext fieldContext) {
      var fldResolver = fieldContext.MappedField.Resolver;
      var fldDef = fldResolver.Field;
      if (fieldContext.ArgValues == null)
        BuildResolverArguments(fieldContext);
      // we might have encountered errors when evaluating args; if so, abort all
      this.AbortIfFailed();
      if (fieldContext.ResolverClassInstance == null)
        await AssignResolverClassInstance(fieldContext, fldResolver);
      // set current parentEntity arg
      var isStatic = fldDef.Flags.IsSet(FieldFlags.Static);
      if (!isStatic) {
        fieldContext.ArgValues[1] = fieldContext.CurrentParentScope.Entity;
      }
      var clrMethod = fldResolver.ResolverMethod.Method;
      var result = clrMethod.Invoke(fieldContext.ResolverClassInstance, fieldContext.ArgValues);
      if (fldResolver.ResolverMethod.ReturnsTask)
        result = await UnwrapTaskResultAsync(fieldContext, fldResolver, (Task)result);
      Interlocked.Increment(ref _requestContext.Metrics.ResolverCallCount);
      // Note: result might be null, but batched result might be set.
      return result;
    }

    // merge with prev method InvokeResolverAsync 
    private object InvokeResolverFunc(FieldContext fieldContext) {
      var propReader = fieldContext.MappedField.Resolver.ResolverFunc;
      var entity = fieldContext.CurrentParentScope.Entity; 
      var result = propReader(entity);
      return result;
    }

    private async Task<object> UnwrapTaskResultAsync(FieldContext fieldContext, FieldResolverInfo fieldResolver, Task task) {
      if (!task.IsCompleted)
        await task; 
      switch(task.Status) {
        
        case TaskStatus.Faulted:
          Exception origExc = task.Exception;
          if(origExc is AggregateException aex)
            origExc = aex.InnerException; //we expect just one exc (we ignore exc.InnerExceptions list)
          AddError(fieldContext, origExc, ErrorCodes.ResolverError);
          Fail();
          return null; 
        
        case TaskStatus.RanToCompletion:
          var resReader = fieldResolver.ResolverMethod.TaskResultReader;
          var result = resReader(task);
          return result;

        case TaskStatus.Canceled:
        default:
          var msg = "Resolver execution canceled.";
          var ex = new Exception(msg);
          AddError(fieldContext, ex, ErrorCodes.Cancelled);
          throw new ResolverException(msg); 
      }
    }

    private void BuildResolverArguments(FieldContext fieldContext) {
      var mappedArgs = fieldContext.MappedField.MappedArgs;
      var argValues = new List<object>();
      // special arguments: context, parent      
      argValues.Add(fieldContext);
      if(!fieldContext.FieldDef.Flags.IsSet(FieldFlags.Static))
        argValues.Add(fieldContext.CurrentParentScope.Entity);
      //regular arguments
      for (int i = 0; i < mappedArgs.Count; i++) {
        var arg = mappedArgs[i];
        var argValue = SafeEvaluateArg(fieldContext, arg);
        argValues.Add(argValue);
      }
      fieldContext.ArgValues = argValues.ToArray();
    }

    private object SafeEvaluateArg(FieldContext fieldContext, MappedArg arg) {
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
    private async Task AssignResolverClassInstance(FieldContext fieldCtx, FieldResolverInfo fieldResolver) {
      var resClassType = fieldResolver.ResolverMethod.ResolverClass.Type;
      object resInstance = null; 
      if (_resolverInstances.Count == 1 && _resolverInstances[0].GetType() == resClassType) // fast track
        resInstance = _resolverInstances[0];
      else 
        resInstance = _resolverInstances.FirstOrDefault(r => r.GetType() == resClassType);
      if(resInstance == null) {
        resInstance = Activator.CreateInstance(resClassType);
        if(resInstance is IResolverClass iRes)
          iRes.BeginRequest(_requestContext);
        if (resInstance is IResolverClassAsync iResAsync)
          await iResAsync.BeginRequestAsync(_requestContext);
        _resolverInstances.Add(resInstance); 
      }
      fieldCtx.ResolverClassInstance = resInstance;
    }

  } //class
}
