using System;
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
      var allChildScopeKVs = scope
                    .Where(kv => kv.Value is OutputObjectScope) //Only child scopes, from sel subsets; not primitive values or arrays
                    .ToList();
      var groupsToMerge = allChildScopeKVs
                    .GroupBy(kv => kv.Key)  // group by key
                    .Where(g => g.Count() > 1) // and find duplicates
                    .Select(g => g.Select(v => (OutputObjectScope)scope).ToArray())
                    .ToList();
      // found groups: merge all into first
      foreach(var group in groupsToMerge) {
        MergeIntoFirst(group);
        // traverse the tree recursively, to merge child branches 
        var scope0 = group[0];
        MergeFields(scope0);
      }

      // Add processing Array child fields
      !!

      // for the rest - traverse the response tree to merge more inside the tree
      foreach (var kv in allChildScopeKVs) {
        var child = (OutputObjectScope)kv.Value;
        if (child.Merged)
          continue; 
        MergeFields(child); 
      }
    }

    private static void MergeIntoFirst(IList<OutputObjectScope> scopes) {
      var scope0 = scopes[0];
      for (int i = 1; i < scopes.Count; i++) {
        var sc = scopes[i];
        scope0.AddFrom(sc);
        sc.Merged = true; //mark it as merged, so it will be ignored by serializer (not sent by enumerator)
        sc.Clear(); //empty it to free memory
      }

    } // method

  } //class

}
