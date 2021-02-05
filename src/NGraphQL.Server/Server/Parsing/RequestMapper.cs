using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Parsing {

  /// <summary>RequestMapper takes request tree and maps its objects to API model; for ex: selection field is mapped to field definition</summary>
  public partial class RequestMapper {
    GraphQLApiModel _model;
    RequestContext _requestContext;
    GraphQLOperation _currentOp;

    #region pending selection sets list
    // for certain reasons (coming from Fragments and their inter-dependencies) we traverse 
    // the selection tree in wide-first manner; so when we encounter a field with a selection subset,
    // we do not go in immediately, but add the info to this pending list; and the will process it 
    // in the next round
    IList<PendingSelectionSet> _pendingSelectionSets = new List<PendingSelectionSet>();

    class PendingSelectionSet {
      public SelectionSubset SubSet;
      public TypeDefBase OverType;
      public IList<RequestDirective> Directives;
    }
    #endregion

    public RequestMapper(RequestContext requestContext) {
      _requestContext = requestContext;
      _model = _requestContext.ApiModel;
    }



    public void MapAndValidateRequest() {
      Fragments_MapValidate();
      if (_requestContext.Failed)
        return;
      foreach (var op in _requestContext.ParsedRequest.Operations) {
        op.OperationTypeDef = _model.GetOperationDef(op.OperationType);
        _currentOp = op;
        CalcVariableDefaultValues(op);
        MapOperation(op);
      }
      _currentOp = null;
    }

    private void CalcVariableDefaultValues(GraphQLOperation op) {
      foreach (var varDef in op.Variables) {
        if (varDef.ParsedDefaultValue == null)
          continue;
        var inpDef = varDef.InputDef;        
        var typeRef = inpDef.TypeRef;
        var eval = GetInputValueEvaluator(inpDef, varDef.ParsedDefaultValue, typeRef);
        if (!eval.IsConst()) {
          // somewhere inside there's reference to variable, this is not allowed
          AddError($"Default value cannot reference variables.", varDef);
          continue;
        }
        var value = eval.GetValue(_requestContext);
        if (value != null && value.GetType() != typeRef.TypeDef.ClrType) {
          // TODO: fix that, add type conversion, for now throwing exception; or maybe it's not needed, value will be converted at time of use
          // but spec also allows auto casting like  int => int[]
          AddError($"Detected type mismatch for default value '{value}' of variable {varDef.Name} of type {typeRef.Name}", varDef);
        }
        inpDef.DefaultValue = value;
        inpDef.HasDefaultValue = true; 
      } // foreach varDef
    }

    private void MapOperation(GraphQLOperation op) {
      MapSelectionSubSet(op.SelectionSubset, op.OperationTypeDef, op.Directives);
      if (_pendingSelectionSets.Count > 0)
        MapPendingSelectionSubsets(); 
    }

    private void MapPendingSelectionSubsets() {
      while (_pendingSelectionSets.Count > 0) {
        if (_requestContext.Failed)
          return;
        var oldSets = _pendingSelectionSets;
        _pendingSelectionSets = new List<PendingSelectionSet>();
        foreach (var info in oldSets)
          MapSelectionSubSet(info.SubSet, info.OverType, info.Directives);
      }// while
    }

    private void MapSelectionSubSet(SelectionSubset selSubset, TypeDefBase typeDef, IList<RequestDirective> directives) {
      switch(typeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          // that should never happen
          AddError($"Scalar or Enum may not have selection subset", selSubset);
          break;

        case ObjectTypeDef objTypeDef:
          MapObjectSelectionSubset(selSubset, objTypeDef, directives);
          break;

        case InterfaceTypeDef intTypeDef:
          foreach(var objType in intTypeDef.PossibleTypes)
            MapObjectSelectionSubset(selSubset, objType, directives);
          break;

        case UnionTypeDef unionTypeDef:
          foreach(var objType in unionTypeDef.PossibleTypes)
            MapObjectSelectionSubset(selSubset, objType, directives, isForUnion: true);
          break;
      }
    }

    // Might be called for ObjectType or Interface (for intf - just to check fields exist)
    private void MapObjectSelectionSubset(SelectionSubset selSubset, ObjectTypeDef objectTypeDef, IList<RequestDirective> directives, bool isForUnion = false) {
      var mappedFields = MapSelectionItems(selSubset.Items, objectTypeDef, directives, isForUnion);
      selSubset.MappedItemSets.Add(new MappedObjectItemSet() { ObjectTypeDef = objectTypeDef, Items = mappedFields });
    }

    // Might be called for ObjectType or Interface (for intf - just to check fields exist)
    private List<MappedSelectionItem> MapSelectionItems(IList<SelectionItem> selItems, ObjectTypeDef objectTypeDef,
              IList<RequestDirective> ownerDirectives = null, bool isForUnion = false) {
      var mappedItems = new List<MappedSelectionItem>();
      foreach(var item in selItems) {

        switch(item) {
          case SelectionField selFld:
            var fldDef = objectTypeDef.Fields.FirstOrDefault(f => f.Name == selFld.Name);
            if(fldDef == null) {
              // if field not found, the behavior depends if it is a union; it is error for a union
              if(!isForUnion)
                AddError($"Field '{selFld.Name}' not found on type '{objectTypeDef.Name}'.", selFld);
              continue; 
            }
            var mappedArgs = MapArguments(selFld.Args, fldDef.Args, selFld);
            var mappedFld = new MappedField(selFld, fldDef, mappedArgs);
            AddRuntimeModelDirectives(mappedFld);
            AddRuntimeRequestDirectives(mappedFld); 
            mappedItems.Add(mappedFld);
            ValidateMappedFieldAndProcessSubset(mappedFld);
            break;

          case FragmentSpread fs:
            var mappedSpread = MapFragmentSpread(fs, objectTypeDef, isForUnion);
            if (mappedSpread != null) {// null is indicator of error
              AddRuntimeRequestDirectives(mappedSpread);
              mappedItems.Add(mappedSpread);
            }
            break;
        }//switch

        var allDirs = ownerDirectives.MergeLists(item.Directives);


      } //foreach item
      return mappedItems; 
    }

    private void AddRuntimeRequestDirectives(MappedSelectionItem mappedItem) {
      var selItem = mappedItem.Item;
      // request directives first; map dir args
      if (selItem.Directives != null) {
        foreach (var dir in selItem.Directives) {
          dir.MappedArgs = MapArguments(dir.Args, dir.Def.Args, dir);
          mappedItem.AddDirective(new RuntimeRequestDirective(dir));
        }
      }
    }

    private void AddRuntimeModelDirectives(MappedField mappedField) {
      var fldDef = mappedField.FieldDef;
      if (fldDef.HasDirectives())
        foreach (ModelDirective fldDir in fldDef.Directives)
          mappedField.AddDirective(new RuntimeModelDirective(fldDir));
      var typeDef = fldDef.TypeRef.TypeDef;
      if (typeDef.HasDirectives())
        foreach (ModelDirective tdir in typeDef.Directives)
          mappedField.AddDirective(new RuntimeModelDirective(tdir));
    }

    private MappedFragmentSpread MapFragmentSpread(FragmentSpread fs, ObjectTypeDef objectTypeDef, bool isForUnion) {
      // if it is not inline fragment, it might need to map to FragmentDef; inline fragments are auto-mapped at construction
      if (fs.Fragment == null)
        fs.Fragment = GetFragmentDef(fs.Name);
      if (fs.Fragment == null) {
        AddError($"Fragment {fs.Name} not defined.", fs);
        return null;
      }
      // inline fragments are mapped in-place, here.
      // we need to map them here, once we know the target type
      if (fs.IsInline) {
        var onTypeRef = fs.Fragment.OnTypeRef;
        var skip = onTypeRef != null && onTypeRef.TypeDef != objectTypeDef;
        if (skip)
          return null; 
        MapObjectSelectionSubset(fs.Fragment.SelectionSubset, objectTypeDef, fs.Directives, isForUnion);
      }
      // there must be mapped field set now
      var mappedFragmItemSet = fs.Fragment.SelectionSubset.MappedItemSets.FirstOrDefault(fset => fset.ObjectTypeDef == objectTypeDef);
      if (mappedFragmItemSet == null) {
        AddError($"FATAL: Could not retrieve mapped item list for fragment spread {fs.Name}", fs, ErrorCodes.ServerError);
        return null;
      }
      var mappedSpread = new MappedFragmentSpread(fs, mappedFragmItemSet.Items);
      return mappedSpread;
    }

    private void ValidateMappedFieldAndProcessSubset(MappedField mappedField) {
      var typeDef = mappedField.FieldDef.TypeRef.TypeDef;
      var selField = mappedField.Field;
      var selSubset = selField.SelectionSubset;
      var typeName = typeDef.Name; 
      switch(typeDef) {
        case ScalarTypeDef _:
        case EnumTypeDef _:
          if (selSubset != null)
            AddError($"Field '{selField.Key}' of type '{typeName}' may not have a selection subset.", selSubset);
          break;
        
        default: // ObjectType, Union or Interface 
          if (selSubset == null) {
            AddError($"Field '{selField.Key}' of type '{typeName}' must have a selection subset.", selField);
            return; 
          }
          _pendingSelectionSets.Add(new PendingSelectionSet() {
            SubSet = selSubset, OverType = typeDef, Directives = selField.Directives
          });
          break;
      }
    }

    private void AddError(string message, RequestObjectBase item, string errorType = ErrorCodes.BadRequest) {
      var path = item.GetRequestObjectPath();
      var err = new GraphQLError(message, path, item.SourceLocation, errorType);
      _requestContext.AddError(err);
    }

  } // class
}
