using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Model.Request;

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

    private static void AddIncludedMappedFieldsRec(RequestContext requestContext, IList<MappedSelectionItem> items,
                IList<MappedField> result, ref bool hasIncludeSkip) {
      foreach(var item in items) {
        var dirs = item.Item.Directives;
        if (dirs != null && dirs.Count > 0) {
          if (item.HasDirectives && !requestContext.ShouldInclude(item, ref hasIncludeSkip))
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
      if (!mappedItem.HasDirectives)
        return true;
      foreach (var dir in mappedItem.Directives) {
        var action = dir.Def.Handler as ISkipDirectiveAction;
        if (action == null)
          continue;
        hasIncludeSkip = true; 
        var argValues = dir.GetArgValues(requestContext);
        if (action.ShouldSkip(requestContext, mappedItem, argValues)) 
          return false;
      }
      return true;
    }

  }
}
