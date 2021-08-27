using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;
using NGraphQL.Introspection;
using System;

namespace NGraphQL.Server.Mapping {

  /// <summary>RequestMapper takes request tree and maps its objects to API model; for ex: selection field is mapped to field definition</summary>
  public partial class RequestMapper {

    private void MapOperation(GraphQLOperation op) {
      _currentOp = op; 
      MapSelectionSubSet(op.SelectionSubset, op.OperationTypeDef);
    }

    private void MapSelectionSubSet(SelectionSubset selSubset, TypeDefBase typeDef) {
      switch(typeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          // that should never happen
          AddError($"Scalar or Enum may not have a selection subset", selSubset);
          break;

        case ObjectTypeDef objTypeDef:
          MapObjectSelectionSubset(selSubset, objTypeDef);
          break;

        case InterfaceTypeDef intTypeDef:
          foreach(var objType in intTypeDef.PossibleTypes)
            MapObjectSelectionSubset(selSubset, objType);
          break;

        case UnionTypeDef unionTypeDef:
          foreach(var objType in unionTypeDef.PossibleTypes)
            MapObjectSelectionSubset(selSubset, objType, isForUnion: true);
          break;
      }
    }
    
    // Might be called for ObjectType or Interface (for intf - just to check fields exist)
    private void MapObjectSelectionSubset(SelectionSubset selSubset, ObjectTypeDef objectTypeDef, bool isForUnion = false) {
      // Map arguments on fields, add directives, map fragments 
      foreach (var item in selSubset.Items) {
        AddRuntimeRequestDirectives(item);
        switch (item) {
          case SelectionField selFld:
            var fldDef = objectTypeDef.Fields[selFld.Name];
            if (fldDef == null) {
              // if field not found, the behavior depends if it is a union; it is error for a union
              if (!isForUnion)
                AddError($"Field '{selFld.Name}' not found on type '{objectTypeDef.Name}'.", selFld);
              continue;
            }
            selFld.MappedArgs = MapArguments(selFld.Args, fldDef.Args, selFld);
            AddRuntimeModelDirectives(fldDef);
            MapSelectionFieldSubsetIfPresent(selFld, fldDef.TypeRef.TypeDef);
            break;

          case FragmentSpread fspread:
            // Named fragment refs are NOT set by parser; parser sets only Inline fragmDefs; we need to match named fragms here
            if (!fspread.IsInline && fspread.Fragment == null) { 
              fspread.Fragment = GetFragmentDef(fspread.Name);
              if (fspread.Fragment == null)
                AddError($"Fragment {fspread.Name} not defined.", fspread);
            }
            break; 
        }//switch
      } //foreach item

      if (_requestContext.Failed)
        return; 

      // Now create mappings for all possible entity types
      foreach (var typeMapping in objectTypeDef.Mappings) {
        // It is possible mapped set already exists (with unions and especially fragments)
        var existing = selSubset.MappedSubSets.FirstOrDefault(ms => ms.Mapping == typeMapping);
        if(existing != null)
          continue; 
        var mappedItems = new List<MappedSelectionItem>();
        foreach (var item in selSubset.Items) {

          switch (item) {
            case SelectionField selFld:
              var fldDef = typeMapping.TypeDef.Fields[selFld.Name];
              if (fldDef == null)
                // it is not error, it should have been caught earlier; it is unmatch for union
                continue;
              var fldResolver = typeMapping.GetResolver(fldDef);
                //.FirstOrDefault(fr => fr.Field.Name == selFld.Name);
              var mappedFld = new MappedSelectionField(selFld, fldResolver);
              mappedItems.Add(mappedFld);
              break;

            case FragmentSpread fs:
              var onType = fs.Fragment.OnTypeRef?.TypeDef;
              var skip = onType != null && onType.Kind == TypeKind.Object && onType != objectTypeDef;
              if (skip)
                continue;
              if (fs.IsInline)  // only inline fragments should be mapped from here; named fragments are mapped separately, upfront
                MapObjectSelectionSubset(fs.Fragment.SelectionSubset, objectTypeDef, isForUnion);
              var mappedSpread = new MappedFragmentSpread(fs);
              mappedItems.Add(mappedSpread);
              break;
          }//switch

        } //foreach item

        selSubset.MappedSubSets.Add(new MappedSelectionSubSet() { Mapping = typeMapping, MappedItems = mappedItems });
      } //foreach typeMapping
    }

    private FragmentDef GetFragmentDef(string name) {
      var fragm = _requestContext.ParsedRequest.Fragments.FirstOrDefault(f => f.Name == name);
      return fragm; 
    } 

    private void MapSelectionFieldSubsetIfPresent(SelectionField selField, TypeDefBase fieldType) {
      var selSubset = selField.SelectionSubset;
      switch(fieldType) {
        case ScalarTypeDef sc:
          if(selSubset != null && !sc.Scalar.CanHaveSelectionSubset)
            AddError($"Field '{selField.Key}' of type '{fieldType.Name}' may not have a selection subset.", selSubset);
          break;

        case EnumTypeDef _:
          if (selSubset != null)
            AddError($"Field '{selField.Key}' of enum type '{fieldType.Name}' may not have a selection subset.", selSubset);
          break;
        
        default: // ObjectType, Union or Interface 
          if (selSubset == null) {
            AddError($"Field '{selField.Key}' of type '{fieldType.Name}' must have a selection subset.", selField);
            return; 
          }
          MapSelectionSubSet(selSubset, fieldType);
          break;
      }
    }


  } // class
}
