using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using NGraphQL.Model;
using NGraphQL.Model.Core;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

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
        if (!varDef.HasDefaultValue)
          continue;

        var typeRef = varDef.TypeRef;
        var eval = GetInputValueEvaluator(varDef.ParsedDefaultValue, typeRef);
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
        varDef.DefaultValue = value;
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
      selSubset.MappedFieldSets.Add(new MappedObjectFieldSet() { ObjectTypeDef = objectTypeDef, Fields = mappedFields });
    }

    // Might be called for ObjectType or Interface (for intf - just to check fields exist)
    private List<MappedField> MapSelectionItems(IList<SelectionItem> selItems, ObjectTypeDef objectTypeDef,
              IList<RequestDirective> ownerDirectives = null, bool isForUnion = false) {
      var mappedFields = new List<MappedField>();
      foreach(var item in selItems) {
        if(item.Directives.HasAny()) {
          foreach(var dir in item.Directives)
            if(dir.MappedArgs == null)
              dir.MappedArgs = MapArguments(dir.Args, dir.Def.Args, dir);
        }
        var allDirs = ownerDirectives.MergeLists(item.Directives);

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
            var mappedFld = new MappedField() {
              FieldDef = fldDef, SelectionField = selFld, Args = mappedArgs, 
              IncludeSkipDirectives = SelectIncludeDirectives(allDirs)
            };
            mappedFields.Add(mappedFld);
            ValidateMappedFieldAndProcessSubset(mappedFld);
            break;

          case FragmentSpread fs:
            var mappedFragmFields = MapFragmentSpread(fs, objectTypeDef, allDirs, isForUnion);
            if (mappedFragmFields != null) // null is indicator of error
              mappedFields.AddRange(mappedFragmFields);
            break;
        }//switch
      }
      return mappedFields; 
    }

    private IList<MappedField> MapFragmentSpread(FragmentSpread fs, ObjectTypeDef objectTypeDef, 
                  IList<RequestDirective> ownerDirectives, bool isForUnion) {
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
          return MappedField.EmptyList; 
        MapObjectSelectionSubset(fs.Fragment.SelectionSubset, objectTypeDef, fs.Directives, isForUnion);
      }
      // there must be mapped field set now
      var mappedFragmFieldSet = fs.Fragment.SelectionSubset.MappedFieldSets.FirstOrDefault(fset => fset.ObjectTypeDef == objectTypeDef);
      if (mappedFragmFieldSet == null) {
        AddError($"FATAL: Could not retrieve mapped field list for fragment spread {fs.Name}", fs, ErrorTypes.ServerError);
        return null;
      }
      return mappedFragmFieldSet.Fields;
    }

    private IList<RequestDirective> SelectIncludeDirectives(IList<RequestDirective> dirs) {
      if(dirs == null || dirs.Count == 0)
        return RequestDirective.EmptyList; 
      var incDirs = dirs.Where(d => d.Def is IIncludeSkipDirectiveDef).ToArray();
      return incDirs;
    }

    private void ValidateMappedFieldAndProcessSubset(MappedField mappedField) {
      var typeDef = mappedField.FieldDef.TypeRef.TypeDef;
      var selField = mappedField.SelectionField;
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

    private void AddError(string message, RequestObjectBase item, string errorType = ErrorTypes.BadRequest) {
      var path = item.GetRequestObjectPath();
      var err = new GraphQLError(message, path, item.Location, errorType);
      _requestContext.AddError(err);
    }

  } // class
}
