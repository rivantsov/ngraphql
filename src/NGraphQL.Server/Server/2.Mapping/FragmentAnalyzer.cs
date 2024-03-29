﻿using System;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.CodeFirst;

namespace NGraphQL.Server.Mapping {

  public class FragmentAnalyzer {
    RequestContext _requestContext;
    IList<FragmentDef> _namedFragments; // non inline fragments


    public FragmentAnalyzer(RequestContext context) {
      _requestContext = context;
    }

    private void AddError(string message, RequestObjectBase item, string errorType = ErrorCodes.BadRequest) {
      _requestContext.AddError(message, item, errorType);
    }


    private FragmentDef GetFragmentDef(string name) {
      var fragm = _requestContext.ParsedRequest.Fragments.FirstOrDefault(fd => fd.Name == name);
      return fragm;
    }


    // analyzes refs to fragments inside fragments; checks circular refs
    public void Analyze() {
      var allFragments = _requestContext.ParsedRequest.Fragments;
      if (allFragments.Count == 0)
        return; 
      // select named fragments (exclude inline fragments) 
      _namedFragments = allFragments.Where(f => !f.IsInline).ToList();
      // Map references in fragmentSpreads, OnType references
      Fragments_MapOnTypeReferences();
      if (_requestContext.Failed)
        return;
      Fragments_MapValidateFragmentSpreads();
      if (_requestContext.Failed)
        return;
      // Validate details of each fragment 
      Fragments_ValidateFragmentFieldsForTargetType();
      if (_requestContext.Failed)
        return;
    }

    private void Fragments_MapOnTypeReferences() {
      var typesByName = _requestContext.Server.Model.TypesByName;
      foreach (var fragm in _requestContext.ParsedRequest.Fragments) {
        if (fragm.OnTypeRef == null)
          continue; 
        // resolve OnType reference
        var onTypeName = fragm.OnTypeRef.Name;
        if (!typesByName.TryGetValue(onTypeName, out var onTypeDef)) {
          AddError($"On-type target type '{onTypeName}' not defined.", fragm.OnTypeRef);
          continue;
        }
        // check it is a proper type
        var typeIsOk = onTypeDef.IsOneOf(TypeKind.Object, TypeKind.Interface, TypeKind.Union);
        if (!typeIsOk) {
          AddError($"Fragment cannot be defined on type '{onTypeDef.Name}'.", fragm.OnTypeRef);
          continue;
        }
        fragm.OnTypeRef.TypeDef = onTypeDef;
      }
    }

    // Note that we consider ONLY fragment refs in 'top' selection fields,
    // and ignore refs in selection subsets of object fields. It is OK for a fragment to self-reference if it is 
    // inside sub-selection set of some top-level field. This does not necessarily cause endless ref loop when 
    // unfolding the field data at runtime, as at some point the field might return null, 
    // so data chains are not 'endless'. If the chain is in fact endless, then it will be cut-off at runtime 
    // when reaching the request depth limit (per quota).
    private void Fragments_MapValidateFragmentSpreads() {
      // map initial list of directly referenced fragments. 
      foreach (var fragm in _namedFragments) {
        foreach (var item in fragm.SelectionSubset.Items) {
          if (!(item is FragmentSpread fs))
            continue;
          fs.Fragment = GetFragmentDef(fs.Name);
          if (fs.Fragment == null) {
            AddError($"Fragment '{fs.Name}' not defined (in fragment '{fragm.Name}').", fs);
            continue;
          }
          if (fs.Fragment == fragm) {
            AddError($"Fragment {fragm.Name} may not reference itself", fs);
            continue;
          }
          if (fragm.UsesFragments.Contains(fs.Fragment)) {
            AddError($"Fragment '{fs.Name}' is referenced more than once (in fragment '{fragm.Name}').", fs);
            continue;
          }
          fragm.UsesFragments.Add(fs.Fragment);

          Fragments_CheckFragmentSpreadCompatible(fs, fragm.OnTypeRef.TypeDef);
        }
      } //foreach fragm
      if (_requestContext.Failed)
        return;
      // Build closure of fragment references, to detect fragment self-referencing through other fragments
      foreach (var fragm in _namedFragments) {
        Fragments_AddDeepFragmentRefsRec(fragm, fragm.UsesFragments);
      }
    } //method

    private bool Fragments_CheckFragmentSpreadCompatible(FragmentSpread fragmentSpread, TypeDefBase parentType) {
      var fragmOnTypeDef = fragmentSpread.Fragment.OnTypeRef.TypeDef;
      if (parentType == fragmOnTypeDef)
        return true; //trivial case 
      var fragmName = fragmentSpread.Name;
      switch (parentType) {

        case ComplexTypeDef complexParentType:
          // Parent type (PT) is interface or object type. 
          // Fragment's OnType (FOT) 
          //   1. FOT is interface - then PT must implement it
          //   2. FOT is object - it must match PT (be the same type); this we already checked
          var implements = (fragmOnTypeDef is InterfaceTypeDef) && complexParentType.Implements.Contains(fragmOnTypeDef);
          if (implements)
            return true; 
          AddError($"Fragment ref '{fragmName}': fragment is defined on type '{fragmOnTypeDef.Name}'" +
                    $" which is not compatible with type '{parentType.Name}'.",
                    fragmentSpread);
          return false;

        case UnionTypeDef parUnionType:
          // fragment's on-type must be one of the unioned types; of if fragm is on interface, implemented by at least one of the types
          if (fragmOnTypeDef is InterfaceTypeDef fragmIntType) {
            if (!parUnionType.PossibleTypes.Any(t => t.Implements.Contains(fragmIntType))) {
              AddError($"Fragment ref {fragmName}: fragment is defined on interface '{fragmIntType.Name}' which is " +
                       $"not implemented by any of the unioned types in '{parentType.Name}'.", fragmentSpread);
              return false;
            }
          } else {
            // fragm is on object type
            if (!parUnionType.PossibleTypes.Contains(fragmOnTypeDef)) {
              AddError($"Fragment ref {fragmName}: fragment is defined on type '{fragmOnTypeDef.Name}' which is not " +
                        $"one of the unioned types in '{parentType.Name}'.", fragmentSpread);
              return false;
            }
          }
          break;
      }
      return true;
    }

    private void Fragments_AddDeepFragmentRefsRec(FragmentDef topFragment, IList<FragmentDef> refsToCheck) {
      foreach (var refFragmDef in refsToCheck) {
        if (refFragmDef == topFragment) {
          AddError($"Fragment {refFragmDef.Name} is self-referencing, possibly through circular references with other fragments.",
            refFragmDef);
          return;
        }
        if (!topFragment.UsesFragmentsAll.Contains(refFragmDef)) {
          topFragment.UsesFragmentsAll.Add(refFragmDef);
          Fragments_AddDeepFragmentRefsRec(topFragment, refFragmDef.UsesFragments);
        }
      }
    }

    private void Fragments_ValidateFragmentFieldsForTargetType() {
      var fragments = _requestContext.ParsedRequest.Fragments;
      foreach (var fragm in fragments) {
        if (fragm.OnTypeRef == null)
          continue; // inline fragm might be without on-type
        switch (fragm.OnTypeRef.TypeDef) {
          case ComplexTypeDef fdDefsCont: 
            //object type or interface - we require that all fragment fields are present in target object/interface
            foreach (var selItem in fragm.SelectionSubset.Items)
              if (selItem is SelectionField selFld) {
                var fldDef = fdDefsCont.Fields.FirstOrDefault(f => f.Name == selFld.Name);
                if (fldDef == null)
                  AddError($"Fragment {fragm.Name}: field '{selFld.Name}' not defined on type '{fdDefsCont.Name}'.", selFld);
              }
            break;

          case UnionTypeDef unionTypeDef:
            // we require that every fragment field is in at least one of union's types  
            var allFieldNames = new HashSet<string>(unionTypeDef.PossibleTypes.SelectMany(t => t.Fields.Select(f => f.Name)));
            foreach (var selItem in fragm.SelectionSubset.Items)
              if (selItem is SelectionField selFld && !allFieldNames.Contains(selFld.Name))
                AddError($"Fragment {fragm.Name}: field '{selFld.Name}' not defined" +
                         $" on any of the types in union '{unionTypeDef.Name}'.", selFld);
            break;
        }//switch
      } //foreach fragm
    }

  }
}
