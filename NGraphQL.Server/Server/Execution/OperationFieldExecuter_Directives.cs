using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Server.Execution {

  partial class OperationFieldExecuter {
    
    // returns list of all fields (with expanded fragments), but with regard to @include/@skip directives
    private IList<MappedField> GetIncludedMappedFields(MappedObjectItemSet outSet) {
      var mappedFields = outSet.StaticMappedFields;
      if (mappedFields != null)
        return mappedFields;
      mappedFields = new List<MappedField>();
      bool hasIncludeSkip = false; 
      AddIncludedMappedFieldsRec(outSet.Items, mappedFields, ref hasIncludeSkip);
      if (!hasIncludeSkip)
        outSet.StaticMappedFields = mappedFields;
      return mappedFields; 
    }

    private void AddIncludedMappedFieldsRec(IList<MappedSelectionItem> items, IList<MappedField> result, ref bool hasIncludeSkip) {
      foreach(var item in items) {
        var dirs = item.Item.Directives;
        if (dirs != null && dirs.Count > 0) {
          if (!ShouldInclude(item, ref hasIncludeSkip))
            continue; 
        }
        switch (item) {
          case MappedField fld:
            result.Add(fld);
            continue;
          case MappedFragmentSpread spread:
            AddIncludedMappedFieldsRec(spread.Items, result, ref hasIncludeSkip);
            continue; 
        }
      }
    }

    private bool ShouldInclude(MappedSelectionItem mappedItem, ref bool hasIncludeSkip) {
      var reqDirs = mappedItem.Item.Directives;
      if (reqDirs == null || reqDirs.Count == 0)
        return true;
      foreach (var reqDir in reqDirs) {
        var action = GetRequestDirectiveAction<ISkipDirectiveAction>(reqDir);
        if (action == null)
          continue;
        hasIncludeSkip = true; 
        if (action.ShouldSkip(_requestContext, mappedItem)) 
          return false;
      }
      return true;
    }

    public TAction GetRequestDirectiveAction<TAction>(RequestDirective dir) where TAction : class {
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
        argValues[i] = eval.GetValue(_requestContext);
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
