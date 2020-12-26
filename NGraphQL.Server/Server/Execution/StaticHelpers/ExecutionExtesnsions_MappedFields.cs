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
        var action = reqDir.Def.Handler as ISkipDirectiveAction;
        if (action == null)
          continue; 
        var argValues = requestContext.GetRequestDirectiveArgValues(reqDir);
        if (action.ShouldSkip(requestContext, mappedItem, argValues)) 
          return false;
      }
      return true;
    }

    private static object[] _emptyArgValues = new object[] { };

    internal static object[] GetRequestDirectiveArgValues(this RequestContext requestContext, RequestDirective dir) {
      if (dir.StaticArgValues != null)
        return dir.StaticArgValues; 
      // fast path - parameterless directive
      if (dir.Args.Count == 0) {
        dir.StaticArgValues = _emptyArgValues;
        return dir.StaticArgValues;
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
      if (!hasVars)
        dir.StaticArgValues = argValues;
      return argValues;
    }
  }
}
