using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  public static partial class ExecutionExtensions {

    public static bool IsSet(this GraphQLServerOptions options, GraphQLServerOptions option) {
      return (options & option) != 0;
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

    public static object[] TryEvaluateStaticArgValues(this List<MappedArg> mappedArgs) {
      if (mappedArgs == null || mappedArgs.Count == 0)
        return _emptyArray;
      if (mappedArgs.Any(a => !a.Evaluator.IsConst()))
        return null;   // uses variable, cannot do static args 
      // args do not use vars, so we can evaluate it as static args
      return ExecutionExtensions.EvaluateArgs(null, mappedArgs);
    }



  }
}
