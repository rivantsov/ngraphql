using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace NGraphQL.Server.Execution {

  /*
   Implements merging output fields (data) for the same-named selection fields (same alias or field name). 
  The spec describes the case in the section 6.3.2 (https://spec.graphql.org/October2021/#sec-Field-Collection) 
  Basic rules are:
    1. For simple field with  primitive types like scalars, enums (that do not have selection subsets) - if you have multiple fields with the same key, 
        the output should should contain multiple (key:value) pairs, one for each field
    2. For fields with selection subsets, the fields should be merged into one, with subsets merged - all the way deep down the selection tree. 
        There should be one output value with the key, with data from merged subsets. 

  The spec desribes the CollectFields algorithm that is supposed to be executed early, before resolvers and producing data. 
  We do it differently here, we let the query execute as-is, and then merge the output fields. 
   */
  internal static class OutputObjectScopeFieldMerger {

    public static void MergeFields(OutputObjectScope scope) {
      // using LINQ here might be not very efficient, might change later 

      // Process complex objects first (skip primitive values and arrays)
      var childScopesToMerge = scope.KeysValuePairs
                    .Where(kv => kv.TypeRef.MergeMode == Model.FieldsMergeMode.Object) 
                    .ToList();
      var groupsToMerge = childScopesToMerge
                    .GroupBy(kv => kv.KeyValue.Key)  // group by key
                    .Where(g => g.Count() > 1) // and find duplicates
                    .Select(g => g.Select(v => (OutputObjectScope)v.KeyValue.Value).ToArray())
                    .ToList();
      // found groups: merge all into first
      foreach(var group in groupsToMerge) {
        MergeIntoFirst(group);
      }


      // for the rest - traverse the child tree to merge more inside the subtree
      foreach (var kv in scope.KeysValuePairs) {
        switch (kv.TypeRef.MergeMode) {
          case Model.FieldsMergeMode.None: break;
          
          case Model.FieldsMergeMode.Object:
            var childScope = (OutputObjectScope) kv.KeyValue.Value;
            if (childScope == null || childScope.Merged)
              continue; 
            MergeFields(childScope); 
            break;

          case Model.FieldsMergeMode.Array:
            MergeArray(kv.KeyValue.Value, kv.TypeRef.Rank);
            break;
        }
      }
    }

    private static void MergeArray(object arrObj, int rank) {
      if (arrObj == null) 
        return;
      var arr = (List<object>)arrObj;
      foreach (var elem in arr) {
        if (elem == null) 
          continue;
        if (rank == 1)
          MergeFields((OutputObjectScope)elem);
        else
          MergeArray(elem, rank - 1);
      }
    }

    private static void MergeIntoFirst(IList<OutputObjectScope> scopes) {
      var scope0 = scopes[0];
      for (int i = 1; i < scopes.Count; i++) {
        var sc = scopes[i];
        scope0.KeysValuePairs.AddRange(sc.KeysValuePairs); // copy all to scope0
        sc.Merged = true; //mark it as merged, so it will be ignored by serializer (not sent by enumerator)
        sc.Clear(); //empty it to free memory
      }

    } // method

  } //class

}
