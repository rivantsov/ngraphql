using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  /// <summary>Executes a field of a top-level operation. </summary>
  /// <remarks> We put these methods into a separate class (and not in a RequestHandler) to be able to create 
  /// multiple instances to execute top query fields in parallel. So there will be one RequestHandler and multiple 
  /// OperationFieldExecuter instances. </remarks>
  public partial class OperationFieldExecuter  {
    public string OperationFieldName => _mappedOpField.Field.Name;
    public bool Failed => _failed;
    public object Result;
    public string ResultKey => _mappedOpField.Field.Key;

    RequestContext _requestContext;
    OutputObjectScope _parentScope;
    MappedSelectionField _mappedOpField;
    List<object> _resolverInstances = new List<object>();
    // this is a flag indicating failure of this operation field; we have more global flag in RequestContext,
    //  but it is for ALL operation fields executing concurrently. We track individual oper field in this _failed
    //  flag, so that we know when to abort this field based on its own errors
    private bool _failed;

    // executed field contexts for fields returning objects; these are pending for process result
    // subsets - we do not do it immediately after executing resolver and getting result objects. 
    // Instead, we save them in this list, and process in a later loop, once all 'current' 
    // resolvers are executed. This is all to make it possible to do batched calls (aka Data Loader)
    List<FieldContext> _executedObjectFieldContexts = new List<FieldContext>();

    public OperationFieldExecuter(RequestContext requestContext, MappedSelectionField mappedOpField, OutputObjectScope parentScope) {
      _requestContext = requestContext;
      _parentScope = parentScope;
      _mappedOpField = mappedOpField;
    }

    public async Task ExecuteOperationFieldAsync() {
      try {
        if (_mappedOpField.Field.OnExecuting(_requestContext, out var args) && args.Skip) {
          Result = DBNull.Value; // it's a signal to skip value in output
          return;
        }
        var opFieldContext = new FieldContext(_requestContext, this, _mappedOpField);
        opFieldContext.SetCurrentParentScope(_parentScope);
        var resolverResult = await InvokeResolverAsync(opFieldContext);
        // We do not save result in parent top-level context: we maybe executing in parallel with other top-level fields;
        // we need synchronization(lock), and also op fields might finish out of order. So we save result in a field, and 
        //  RequestHandler will save all results from executers in proper order. 
        //_parentScope.SetValue(_mappedOpField.Field.Key, opOutValue); -- do not do this
        this.Result = opFieldContext.ConvertToOutputValue(resolverResult); //save it for later
        // for fields returning objects, save for further processing of results
        if (opFieldContext.MappedField.Field.SelectionSubset != null)
          _executedObjectFieldContexts.Add(opFieldContext);

        // process object field results until no more
        while (_executedObjectFieldContexts.Count > 0) {
          if (_requestContext.CancellationToken.IsCancellationRequested)
            opFieldContext.ThrowRequestCancelled();
          // save current list, create new one in the field
          var oldFieldContexts = _executedObjectFieldContexts;
          _executedObjectFieldContexts = new List<FieldContext>();
          foreach (var fldCtx in oldFieldContexts) {
            await ExecuteFieldSelectionSubsetAsync(fldCtx);
          }
        }//while
      } finally {
        // notify resolvers about end request
        if (_resolverInstances.Count > 0)
          foreach (var resObj in _resolverInstances)
            (resObj as IResolverClass)?.EndRequest(_requestContext);
      }
    }

    private async Task ExecuteFieldSelectionSubsetAsync(FieldContext parentFieldContext) {
      // all scopes have scope.Entity != null
      var parentScopes = parentFieldContext.AllResultScopes;
      if (parentScopes.Count == 0)
        return;
      var mappedField = parentFieldContext.MappedField;
      var fieldTypeDef = parentFieldContext.FieldDef.TypeRef.TypeDef; 
      var fieldPossibleTypes = fieldTypeDef.PossibleOutTypes;
      var selSubset = mappedField.Field.SelectionSubset;

      // fast path, all the same type;  scopes.Count is always > 0
      var entType = parentScopes[0].Entity.GetType();
      var allSameType = parentScopes.Count == 1 || parentScopes.All(ps => ps.Entity.GetType() == entType);
      if (allSameType) {
        var mappedSubSet = GetMappedSubset(selSubset, fieldPossibleTypes, entType, mappedField.Field);
        await ExecuteMappedSelectionSubsetAsync(mappedSubSet, fieldPossibleTypes, parentScopes);
        return; 
      }
      // multiple entity types
      var scopesByType = parentScopes.GroupBy(s => s.Entity.GetType()).ToList();
      foreach (var grp in scopesByType) {
        entType = grp.Key;
        var mappedSubSet = GetMappedSubset(selSubset, fieldPossibleTypes, entType, mappedField.Field);
        await ExecuteMappedSelectionSubsetAsync(mappedSubSet, fieldPossibleTypes, grp.ToList());
      }
    }

    private async Task ExecuteMappedSelectionSubsetAsync(MappedSelectionSubSet mappedSubSet, IList<ObjectTypeDef> possibleTypes,
                                                                IList<OutputObjectScope> parentScopes) {

      foreach(var mappedItem in mappedSubSet.MappedItems) {

        // TODO: Invoke event to signal execution of directives
        if (mappedItem.Item.OnExecuting(_requestContext, out var args) && args.Skip) {
          continue;
        }

        // if it is a fragment spread, make recursive call to process fragment fields
        if (mappedItem.Item.Kind == SelectionItemKind.FragmentSpread) {
          
          var mappedSpread = (MappedFragmentSpread) mappedItem;
          var objTypeDef = mappedSubSet.Mapping.TypeDef;
          var fragmSelSubset = mappedSpread.Spread.Fragment.SelectionSubset;
          var entType = mappedSubSet.Mapping.EntityType;
          var mappedFragmSubset = GetMappedSubset(fragmSelSubset, possibleTypes, entType, mappedSpread.Spread);
          await ExecuteMappedSelectionSubsetAsync(mappedFragmSubset, possibleTypes, parentScopes); //call self recursively
          continue; 
        }

        // It is a plain field
        var mappedField = (MappedSelectionField) mappedItem;
        var fieldContext = new FieldContext(_requestContext, this, mappedField, parentScopes);
        var selFieldKey = mappedField.Field.Key;

        // Process each scope for the field
        foreach (var scope in parentScopes) {
          if (fieldContext.BatchResultWasSet && scope.ContainsKey(selFieldKey))
            continue; 
          fieldContext.SetCurrentParentScope(scope);
          var fldDef = fieldContext.FieldDef;
          object result = await InvokeResolverAsync(fieldContext);
          // if batched result was not set, set value
          if (!fieldContext.BatchResultWasSet) {
            var outValue = fieldContext.ConvertToOutputValue(result);
            scope.SetValue(selFieldKey, outValue);
          }
        } //foreach scope
        // if there are any non-null object-type results, add this field context to this special list
        //   to execute selection subsets in the next round. 
        if (mappedField.Field.SelectionSubset != null && fieldContext.AllResultScopes.Count > 0) {
          _executedObjectFieldContexts.Add(fieldContext);
        }
      } //foreach mappedItem
    } //method

    private MappedSelectionSubSet GetMappedSubset(SelectionSubset subSet, IList<ObjectTypeDef> objectTypeDefs, Type entityType,
                                                             NamedRequestObject requestObj) {
      var mappedSubset = subSet.MappedSubSets.FirstOrDefault(ms => ms.Mapping.EntityType == entityType &&
                                                               objectTypeDefs.Contains(ms.Mapping.TypeDef));
      if (mappedSubset == null) {
        var types = string.Join(",", objectTypeDefs);
        this._requestContext.AddError($"Failed to find mapping from entity type {entityType} to GraphQLType(s) [{types}].",
          requestObj);
        AbortRequest();
      }
      return mappedSubset;
    }

    private void Fail() {
      _failed = true;
      throw new AbortRequestException();
    }

    public void AbortIfFailed() {
      if (_failed)
        AbortRequest(); 
    }

    public void AbortRequest() {
      throw new AbortRequestException();
    }

    public void AddError(GraphQLError error) {
      _requestContext.AddError(error);
      _failed = true;
    }

    public void AddError(FieldContext fieldContext, Exception ex, string errorType) {
      _failed = true;
      // fire event
      var eventArgs = new OperationErrorEventArgs(_requestContext, this._mappedOpField.Field, ex);
      _requestContext.Server.Events.OnOperationError(eventArgs);
      if (eventArgs.Exception == null)
        return; // event handler cleared error
      // add error
      fieldContext.AddError(ex, errorType);
    }


  }
}