using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  static partial class ExecutionExtensions {
    
    // returns list of all fields (with expanded fragments), but filtered with regard to @include/@skip directives
    internal static IList<MappedSelectionField> GetIncludedMappedFields(this RequestContext requestContext, SelectionSubSetMapping outSet) {
      var mappedFields = outSet.StaticMappedFields;
      if (mappedFields != null)
        return mappedFields;
      mappedFields = new List<MappedSelectionField>();
      bool hasIncludeSkip = false; 
      AddIncludedMappedFieldsRec(requestContext, outSet.MappedItems, mappedFields, ref hasIncludeSkip);
      if (!hasIncludeSkip)
        outSet.StaticMappedFields = mappedFields;
      return mappedFields; 
    }

    private static void AddIncludedMappedFieldsRec(RequestContext requestContext, IList<MappedSelectionItem> mappedSelItems,
                IList<MappedSelectionField> resultMappedFields, ref bool hasIncludeSkip) {
      foreach(var mappedItem in mappedSelItems) {
        if (mappedItem.HasSkipDirectives) {
          hasIncludeSkip = true;
          if (!requestContext.ShouldInclude(mappedItem))
            continue;
        }
        switch (mappedItem) {
          case MappedSelectionField fld:
            resultMappedFields.Add(fld);
            continue;
          case MappedFragmentSpread spread:
            AddIncludedMappedFieldsRec(requestContext, spread.Items, resultMappedFields, ref hasIncludeSkip);
            continue; 
        }
      }
    }

    private static bool ShouldInclude(this RequestContext requestContext, MappedSelectionItem mappedItem) {
      if (!mappedItem.HasSkipDirectives)
        return true;
      foreach (var skipDir in mappedItem.PreparedSkipDirectives) {
        var argValues = skipDir.Directive.GetArgValues(requestContext);
        if (skipDir.Action.ShouldSkip(requestContext, mappedItem, argValues))
          return false;
      }
      return true;
    }

  }
}
