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
  public partial class OperationFieldExecuter : IOperationFieldContext {
    RequestContext _requestContext;
    OutputObjectScope _parentScope; 
    SelectionField _operationField; 
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

    public OperationFieldExecuter(RequestContext requestContext, SelectionField opField, OutputObjectScope parentScope) {
      _requestContext = requestContext;
      _parentScope = parentScope;
      _operationField = opField;
    }

    public async Task ExecuteOperationFieldAsync() {
      try {
        var opFieldContext = new FieldContext(_requestContext, this, _operationField);
        opFieldContext.SetCurrentParentScope(_parentScope);
        var result = await InvokeResolverAsync(opFieldContext);
        var opOutValue = opFieldContext.ConvertToOuputValue(result);
        _parentScope.SetValue(_operationField.Key, opOutValue);
        // for fields returning objects, save for further processing of results
        if (opFieldContext.SelectionField.SelectionSubset != null)
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

    private async Task ExecuteFieldSelectionSubsetAsync(FieldContext fieldContext) {
      // all scopes have sc.Entity field != null
      var scopes = fieldContext.AllResultScopes;
      var outTypeDef = fieldContext.CurrentFieldDef.TypeRef.TypeDef;
      var selSubSet = fieldContext.SelectionField.SelectionSubset;
      switch(outTypeDef.Kind) {
        case TypeKind.Object:
          await ExecuteObjectsSelectionSubsetAsync(fieldContext.SelectionField, scopes, selSubSet);
          return;

        case TypeKind.Interface:
        case TypeKind.Union:
          // group by type, and process each sublist
          var scopesByType = scopes.GroupBy(s => s.Entity.GetType()).ToList();
          foreach(var grp in scopesByType)
            await ExecuteObjectsSelectionSubsetAsync(fieldContext.SelectionField, grp.ToList(), selSubSet);
          return;

        default:
          return; //never happens
      }
    }

    private ObjectTypeDef GetMappedObjectTypeDef(object entity) {
      var typeDef = _requestContext.ApiModel.GetMappedGraphQLType(entity.GetType());
      if(typeDef == null || typeDef.Kind != TypeKind.Object) {
        // TODO: see if it can happen we can throw better error here
      }
      return (ObjectTypeDef)typeDef;
    }

    private async Task ExecuteObjectsSelectionSubsetAsync(SelectionField parentField, 
                 IList<OutputObjectScope> parentScopes, SelectionSubset subSet) {

      foreach(var selItem in subSet.Items) {
        
        // TODO: Invoke event to signal execution of directives
        
        // if it is a fragment spread, make recursive call to process fragment fields
        if (selItem is FragmentSpread spread) {
          await ExecuteObjectsSelectionSubsetAsync(parentField, parentScopes, spread.Fragment.SelectionSubset);
          continue; 
        }

        // It is a plain field
        var selField = (SelectionField) selItem;
        var fieldContext = new FieldContext(_requestContext, this, selField, parentScopes);

        // Process each scope for the field
        foreach (var scope in parentScopes) {
          if (fieldContext.BatchResultWasSet && scope.ContainsKey(selField.Key))
            continue; 
          fieldContext.SetCurrentParentScope(scope);
          var fldDef = fieldContext.CurrentFieldDef;
          object result = null;
          switch (fieldContext.CurrentResolver.ResolverKind) {
            case ResolverKind.CompiledExpression:
              result = InvokeFieldReader(fieldContext, fieldContext.CurrentParentScope.Entity);
              break;
            case ResolverKind.Method:
              result = await InvokeResolverAsync(fieldContext);
              break;
          }
          // if batched result was not set, set value
          if (!fieldContext.BatchResultWasSet) {
            var outValue = fieldContext.ConvertToOuputValue(result);
            scope.SetValue(selField.Key, outValue);
          }
        } //foreach scope
        // if there are any non-null object-type results, add this field context to this special list
        //   to execute selection subsets in the next round. 
        if (selField.SelectionSubset != null && fieldContext.AllResultScopes.Count > 0) {
          _executedObjectFieldContexts.Add(fieldContext);
        }
      } //foreach mappedItem
    } //method

    private object ReadGraphQLObjectValue(FieldDef fldDef, object obj) {
      return ReflectionHelper.GetMemberValue(fldDef.ClrMember, obj);
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

    // IOperationFieldContext members
    public string OperationFieldName => _operationField.Name;

    public bool Failed => _failed;

    public void AddError(GraphQLError error) {
      _requestContext.AddError(error);
      _failed = true;
    }


  }
}