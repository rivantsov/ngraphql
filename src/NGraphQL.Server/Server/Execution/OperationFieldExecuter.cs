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
    MappedSelectionField _operationField; 
    List<object> _resolverInstances = new List<object>();
    // this is a flag indicating failure of this operation field; we have more global flag in RequestContext,
    //  but it is for ALL operation fields executing concurrently. We track individual oper field in this _failed
    //  flag, so that we know when to abort this field based on its own errors
    private bool _failed;

    // executed field contexts for fields returning objects; these are pending for process result
    // subsets - we do not do it immediately after executing resolver and getting result objects. 
    // Instead, we save them in this list, and process in a later loop, once all 'current' 
    // resolvers are executed. This is all to make it possible to do batched calls (aka Data Loader)
    List<SelectionItemContext> _executedObjectFieldContexts = new List<SelectionItemContext>();

    public OperationFieldExecuter(RequestContext requestContext, MappedSelectionField opField, OutputObjectScope parentScope) {
      _requestContext = requestContext;
      _parentScope = parentScope;
      _operationField = opField;
    }

    public async Task ExecuteOperationFieldAsync() {
      try {
        var opFieldContext = new SelectionItemContext(_requestContext, this, _operationField);
        opFieldContext.CurrentScope = _parentScope;
        var result = await InvokeResolverAsync(opFieldContext);
        var opOutValue = opFieldContext.ConvertToOuputValue(result);
        // Safe means with lock, protect concurrent access
        _parentScope.SetValue(_operationField.Index, opOutValue);
        // for fields returning objects, save for further processing of results
        if (opFieldContext.Flags.IsSet(FieldFlags.ReturnsComplexType))
          _executedObjectFieldContexts.Add(opFieldContext);

        // process object field results until no more
        while (_executedObjectFieldContexts.Count > 0) {
          if (_requestContext.CancellationToken.IsCancellationRequested)
            opFieldContext.ThrowRequestCancelled();
          // save current list, create new one in the field
          var oldFieldContexts = _executedObjectFieldContexts;
          _executedObjectFieldContexts = new List<SelectionItemContext>();
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

    private async Task ExecuteFieldSelectionSubsetAsync(SelectionItemContext fieldContext) {
      // all scopes have sc.Entity field != null
      var scopes = fieldContext.AllResultScopes;
      var outTypeDef = fieldContext.FieldDef.TypeRef.TypeDef;
      var selSubSet = fieldContext.MappedField.Field.SelectionSubset;
      switch(outTypeDef.Kind) {
        case TypeKind.Object:
          await ExecuteObjectsSelectionSubsetAsync(fieldContext.MappedField, scopes, selSubSet);
          return;

        case TypeKind.Interface:
        case TypeKind.Union:
          // Map every entity object in scopes to ObjectTypeDef, for every scope
          foreach(var scope in scopes) {
            scope.TypeDef = GetMappedObjectTypeDef(scope.Entity);
          }
          // group by type, and process each sublist
          var scopesByType = scopes.GroupBy(s => s.Entity.GetType()).ToList();
          foreach(var grp in scopesByType)
            await ExecuteObjectsSelectionSubsetAsync(fieldContext.MappedField, grp.ToList(), grp.Key, selSubSet);
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

    private async Task ExecuteObjectsSelectionSubsetAsync(MappedSelectionField parentField, 
                 IList<OutputObjectScope> parentScopes, SelectionSubset subSet) {
      var resolverOutType = parentField.Resolver.OutType;
      var outTypeDef = (ObjectTypeDef) parentField.FieldDef.TypeRef.TypeDef;

      var outSetMapping = subSet.GetMapping(resolverOutType, outTypeDef);

      foreach(var mappedItem in outSetMapping.MappedItems) {
        var fieldContext = new SelectionItemContext(_requestContext, this, mappedItem, parentScopes);
        
        // TODO: Invoke event to signal execution of directives

        // if it is a fragment spread, make recursive call to process fragment fields
        if (mappedItem is MappedFragmentSpread mappedFragm) {
          await ExecuteObjectsSelectionSubsetAsync(parentField, parentScopes, mappedFragm.Spread.Fragment.SelectionSubset);
          continue; 
        }

        // It is a plain field
        var mappedField = (MappedSelectionField) mappedItem;
        var fldDef = fieldContext.FieldDef;

        var returnsComplexType = fldDef.Flags.IsSet(FieldFlags.ReturnsComplexType);
        // Process each scope for the field
        foreach (var scope in parentScopes) {
          if (fieldContext.BatchResultWasSet && scope.HasValue(mappedField.Index))
            continue; 
          fieldContext.CurrentScope = scope;
          object result = null;
          switch (mappedField.Resolver.ResolverKind) {
            case ResolverKind.CompiledExpression:
              result = InvokeFieldReader(fieldContext, fieldContext.CurrentScope.Entity);
              break;
            case ResolverKind.Method:
              result = await InvokeResolverAsync(fieldContext);
              break;
          }
          // if batched result was not set, set value
          if (!fieldContext.BatchResultWasSet) {
            var outValue = fieldContext.ConvertToOuputValue(result);
            scope.SetValue(mappedField.Index, outValue);
          }
        } //foreach scope
        // if there are any non-null object-type results, add this field context to this special list
        //   to execute selection subsets in the next round. 
        if (returnsComplexType && fieldContext.AllResultScopes.Count > 0) {
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
    public string OperationFieldName => _operationField.FieldDef.Name;

    public bool Failed => _failed;

    public void AddError(GraphQLError error) {
      _requestContext.AddError(error);
      _failed = true;
    }


  }
}