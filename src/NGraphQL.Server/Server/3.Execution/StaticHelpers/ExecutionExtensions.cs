using System;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Subscriptions;
using NGraphQL.Subscriptions;

namespace NGraphQL.Server.Execution {

  public static partial class ExecutionExtensions {

    public static bool IsSet(this GraphQLServerOptions value, GraphQLServerOptions flag) {
      return (value & flag) != 0;
    }
    public static bool IsSet(this GraphQLServerFeatures value, GraphQLServerFeatures flag) {
      return (value & flag) != 0;
    }

    private static object[] _emptyArray = Array.Empty<object>();

    public static object[] EvaluateArgs(this RequestContext context, IList<MappedArg> mappedArgs) {
      var argValues = new object[mappedArgs.Count];
      for (int i = 0; i < argValues.Length; i++) {
        argValues[i] = mappedArgs[i].Evaluator.GetValue(context);
      }
      return argValues;
    }

    public static object[] GetArgValues(this RuntimeDirective dir, RequestContext context) {
      return dir.StaticArgValues ?? context.EvaluateArgs(dir.MappedArgs);
    }

    public static object[] TryEvaluateStaticArgValues(this IList<MappedArg> mappedArgs) {
      if (mappedArgs == null || mappedArgs.Count == 0)
        return _emptyArray;
      if (mappedArgs.Any(a => !a.Evaluator.IsConst()))
        return null;   // uses variable, cannot do static args 
      // args do not use vars, so we can evaluate it as static args
      return ExecutionExtensions.EvaluateArgs(null, mappedArgs);
    }

    public static GraphQLServer GetServer(this IRequestContext context) {
      var ctx = (RequestContext)context;
      return ctx.Server; 
    }

    public static SubscriptionContext GetSubscriptionContext(this IRequestContext context) {
      var ctx = (RequestContext)context;
      return ctx.Subscription;
    }
  }
}
