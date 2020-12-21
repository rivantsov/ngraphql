using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Server.Execution {

  static partial class ExecutionExtensions {
    
    // returns list of all fields (with expanded fragments), but with regard to @include/@skip directives
    internal static IList<MappedField> GetIncludedMappedFields(this RequestContext requestContext, MappedObjectItemSet outSet) {
      var mappedFields = outSet.StaticMappedFields;
      if (mappedFields != null)
        return mappedFields;
      mappedFields = new List<MappedField>();
      bool hasIncludeSkip = false; 
      AddIncludedMappedFieldsRec(requestContext, outSet.Items, mappedFields, ref hasIncludeSkip);
      if (!hasIncludeSkip)
        outSet.StaticMappedFields = mappedFields;
      return mappedFields; 
    }

    private static void AddIncludedMappedFieldsRec(RequestContext requestContext, IList<MappedSelectionItem> items, IList<MappedField> result, ref bool hasIncludeSkip) {
      foreach(var item in items) {
        var dirs = item.Item.Directives;
        if (dirs != null && dirs.Count > 0) {
          if (!requestContext.ShouldInclude(item, ref hasIncludeSkip))
            continue; 
        }
        switch (item) {
          case MappedField fld:
            result.Add(fld);
            continue;
          case MappedFragmentSpread spread:
            AddIncludedMappedFieldsRec(requestContext, spread.Items, result, ref hasIncludeSkip);
            continue; 
        }
      }
    }

    private static bool ShouldInclude(this RequestContext requestContext, MappedSelectionItem mappedItem, ref bool hasIncludeSkip) {
      var reqDirs = mappedItem.Item.Directives;
      if (reqDirs == null || reqDirs.Count == 0)
        return true;
      foreach (var reqDir in reqDirs) {
        var action = requestContext.GetRequestDirectiveAction<ISkipDirectiveAction>(reqDir);
        if (action == null)
          continue;
        hasIncludeSkip = true; 
        if (action.ShouldSkip(requestContext, mappedItem)) 
          return false;
      }
      return true;
    }

    internal static TAction GetRequestDirectiveAction<TAction>(this RequestContext requestContext, RequestDirective dir) where TAction : class {
      if (dir.StaticHandler != null)
        return dir.StaticHandler as TAction; 
      // fast path - parameterless directive
      DirectiveHandler handler; 
      if (dir.Args.Count > 0) {
        handler = (DirectiveHandler) Activator.CreateInstance(dir.Def.DirectiveHandlerType);
        dir.StaticHandler = handler;
        return handler as TAction;
      }
      // dir with args
      bool hasVars = false; 
      var argValues = new object[dir.MappedArgs.Count];
      for(int i = 0; i < argValues.Length; i++) {
        var eval = dir.MappedArgs[i].Evaluator;
        argValues[i] = eval.GetValue(requestContext);
        if (!eval.IsConst())
          hasVars = true; 
      }
      handler = (DirectiveHandler) Activator.CreateInstance(dir.Def.DirectiveHandlerType, argValues);
      if (!hasVars)
        dir.StaticHandler = handler;
      return handler as TAction;
    }
  }
}
