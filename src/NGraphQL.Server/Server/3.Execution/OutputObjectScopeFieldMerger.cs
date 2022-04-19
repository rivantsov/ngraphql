using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;

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
        ValidateMergeGroup(group);
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

    private static void ValidateMergeGroup(IList<OutputObjectScope> scopes) {
      var fieldCtx = scopes[0].ParentFieldContext;
      for (int i = 1; i < scopes.Count; i++) {
        var sc = scopes[i];
        var mismatch = fieldCtx.GetMismatchesForMerge(sc.ParentFieldContext);
        if (mismatch == null) continue;
        // found mismatch
        fieldCtx.AddError($"Failed to merge fields for duplicate output key '{fieldCtx.MappedField.Field.Key}'; details: {mismatch} ", ErrorCodes.FieldMergeError);
      }
      fieldCtx.AbortIfErrors();
    } // method

    // returns null if match is OK; otherwise returns error message
    public static string GetMismatchesForMerge(this FieldContext fieldContext, FieldContext other) {
      var selField = fieldContext.MappedField.Field;
      var name1 = fieldContext.FieldDef.Name;
      var name2 = other.FieldDef.Name;
      if (name1 != name2)
        return $"Field names do not match: {name1}, {name2}; cannot merge result object";

      var type1 = fieldContext.FieldDef.TypeRef;
      var type2 = other.FieldDef.TypeRef;
      if (type1 != type2)
        return $"Field types do not match, fields: {type1.Name}, {type2.Name}; cannot merge result object.";

      var hash1 = GetArgsHash(fieldContext);
      var hash2 = GetArgsHash(other);
      if (hash1 != hash2)
        return $"Arguments for fields do not match, cannot merge result object.";
      return null;
    }

    private static int GetArgsHash(FieldContext ctx) {
      var argDefs = ctx.MappedField.Field.Args;
      if (argDefs.Count == 0)
        return 0;
      if (ctx.ArgValues == null || ctx.ArgValues.Length == 0)
        return 0;
      var hash = 0;
      // ctx.ArgValues are args for resolver; arg[0] is fieldContext, skip it. 
      for (int i = 1; i < ctx.ArgValues.Length; i++) {
        var v = ctx.ArgValues[i];
        if (v == null) continue;
        var argName = argDefs[i - 1].Name;
        unchecked { hash = (hash << 1) + argName.GetHashCode() + v.GetHashCode(); }
      }
      return hash;
    }


  } //class
}
