using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Mapping {

  /// <summary>RequestMapper takes request tree and maps its objects to API model; for ex: selection field is mapped to field definition</summary>
  public partial class RequestMapper {

    private void MapOperation(GraphQLOperation op) {
      _currentOp = op; 
      MapSelectionSubSet(op.OperationTypeDef, op.SelectionSubset);
    }

    private void MapSelectionSubSet(TypeDefBase typeDef, SelectionSubset selSubset) {
      switch (typeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          // that should never happen
          AddError($"Scalar or Enum may not have a selection subset", selSubset);
          break;

        case ObjectTypeDef objTypeDef:
          MapObjectTypeSelectionSubset(objTypeDef, selSubset);
          break;

        case InterfaceTypeDef intTypeDef:
          foreach(var possibleTypeDef in intTypeDef.PossibleTypes)
            MapObjectTypeSelectionSubset(possibleTypeDef, selSubset);
          break;

        case UnionTypeDef unionTypeDef:
          selSubset.IsOnUnion = true; 
          foreach(var possibleTypeDef in unionTypeDef.PossibleTypes)
            MapObjectTypeSelectionSubset(possibleTypeDef, selSubset);
          break;
      }

      // directives
      foreach (var item in selSubset.Items) {
        ProcessSelectionItemDirectives(item);
      }

    }

    private void MapObjectTypeSelectionSubset(ObjectTypeDef objectTypeDef, SelectionSubset selSubset) {
      // validate selection fields/fragment spreads 
      foreach (var item in selSubset.Items) {
        switch (item) {
          case SelectionField selFld:
            var fldDef = objectTypeDef.Fields[selFld.Name];
            if (fldDef == null) {
              // if field not found, the behavior depends if it is a union; it is not error for union
              if (!selSubset.IsOnUnion)
                AddError($"Field '{selFld.Name}' not found on type '{objectTypeDef.Name}'.", selFld);
              continue;
            }
            AddFieldTypeDirectives(selFld, fldDef);
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

      // Now create mappings for all possible parent entity types; usually it's just one type
      foreach (var typeMapping in objectTypeDef.Mappings) {
        // It is possible mapped set already exists (with unions and especially fragments)
        if (HasMappedSubsetFor(selSubset, typeMapping))
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
              var mappedArgs = MapArguments(selFld.Args, fldDef.Args, selFld);
              var mappedFld = new MappedSelectionField(selFld, fldResolver, mappedArgs);
              mappedItems.Add(mappedFld);
              break;

            case FragmentSpread fs:
              var onType = fs.Fragment.OnTypeRef?.TypeDef;
              var skip = onType != null && onType.Kind == TypeKind.Object && onType != objectTypeDef;
              if (skip)
                continue;
              if (fs.IsInline) {
                // only inline fragments should be mapped from here; named fragments are mapped separately, upfront
                fs.Fragment.SelectionSubset.IsOnUnion = selSubset.IsOnUnion; //inherit from parent sel subset
                MapObjectTypeSelectionSubset(objectTypeDef, fs.Fragment.SelectionSubset);
              }
              var mappedSpread = new MappedFragmentSpread(fs);
              mappedItems.Add(mappedSpread);
              break;
          }//switch

        } //foreach item

        selSubset.MappedSubSets.Add(new MappedSelectionSubSet() { Mapping = typeMapping, MappedItems = mappedItems });
      } //foreach typeMapping
    }

    private bool HasMappedSubsetFor(SelectionSubset selSubSet, ObjectTypeMapping typeMapping) {
      var mappedSubSets = selSubSet.MappedSubSets;
      // fast path combined with slow path
      return mappedSubSets.Count > 0 && (
        mappedSubSets[0].Mapping == typeMapping || mappedSubSets.Any(ms => ms.Mapping == typeMapping)
        );        
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
          MapSelectionSubSet(fieldType, selSubset);
          break;
      }
    }


  } // class
}
