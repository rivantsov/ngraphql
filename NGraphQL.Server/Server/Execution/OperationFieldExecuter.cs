using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Runtime;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Server.Execution {

  /// <summary>Executes a field of a top-level operation. </summary>
  /// <remarks> We put these methods into a separate class (and not in a RequestHandler) to be able to create 
  /// multiple instances to execute top query fields in parallel. So there will be one RequestHandler and multiple 
  /// OperationFieldExecuter instances. </remarks>
  public partial class OperationFieldExecuter : IOperationFieldContext {
    RequestContext _requestContext;
    OutputObjectScope _parentScope; 
    int _fieldIndex; 
    MappedField _operationField; 
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

    public OperationFieldExecuter(RequestContext requestContext, OutputObjectScope parentScope, int fieldIndex) {
      _requestContext = requestContext;
      _parentScope = parentScope;
      _fieldIndex = fieldIndex;
      _operationField = _parentScope.Fields[_fieldIndex];
    }

    public async Task ExecuteOperationFieldAsync() {
      try {
        var opFieldContext = new FieldContext(_requestContext, this, _operationField, _fieldIndex);
        opFieldContext.CurrentScope = _parentScope;
        var result = await InvokeResolverAsync(opFieldContext);
        var opOutValue = opFieldContext.ConvertToOuputValue(result);
        _parentScope.SetValue(_fieldIndex, opOutValue);
        // for fields returning objects, save for further processing of results
        if (opFieldContext.Flags.IsSet(FieldFlags.ReturnsComplexType))
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
      var scopes = fieldContext.AllResultScopes;
      var typeDef = fieldContext.FieldDef.TypeRef.TypeDef;
      var selSubSet = fieldContext.Field.Field.SelectionSubset;
      switch(typeDef.Kind) {
        case TypeKind.Object:
          await ExecuteObjectsSelectionSubsetAsync(scopes, (ObjectTypeDef)typeDef, selSubSet);
          return;

        case TypeKind.Interface:
        case TypeKind.Union:
          // Map every entity object in scopes to ObjectTypeDef, for every scope
          foreach(var scope in scopes) {
            scope.MappedTypeDef = GetMappedObjectTypeDef(scope);
          }
          // group by ObjectType, and process each sublist
          var scopesByType = scopes.GroupBy(s => s.MappedTypeDef).ToList();
          foreach(var grp in scopesByType)
            await ExecuteObjectsSelectionSubsetAsync(grp.ToList(), grp.Key, selSubSet);
          return;

        default:
          return; //never happens
      }
    }

    private ObjectTypeDef GetMappedObjectTypeDef(OutputObjectScope scope) {
      object entity = scope.Entity;
      var typeDef = _requestContext.ApiModel.GetMappedGraphQLType(entity.GetType());
      if(typeDef == null || typeDef.Kind != TypeKind.Object) {
        // TODO: see if it can happen we can throw better error here
      }
      return (ObjectTypeDef)typeDef;
    }

    private async Task ExecuteObjectsSelectionSubsetAsync(IList<OutputObjectScope> parentScopes,
                                                  ObjectTypeDef objTypeDef, SelectionSubset subSet) {
      var outItemSet = subSet.MappedItemSets.FirstOrDefault(fi => fi.ObjectTypeDef == objTypeDef);
      var mappedFields = _requestContext.GetIncludedMappedFields(outItemSet);
      // init scopes
      foreach (var scope in parentScopes)
        scope.Init(objTypeDef, mappedFields);

      for(int fldIndex = 0; fldIndex < mappedFields.Count; fldIndex++) {
        var mappedField = mappedFields[fldIndex];
        var fieldContext = new FieldContext(_requestContext, this, mappedField, fldIndex, parentScopes);
        foreach (var scope in fieldContext.AllParentScopes) {
          if (fieldContext.BatchResultWasSet && scope.HasValue(fldIndex))
            continue;
          fieldContext.CurrentScope = scope;
          object result = null; 
          switch(mappedField.FieldDef.ExecutionType) {
            case FieldExecutionType.Reader:
              result = InvokeFieldReader(fieldContext, fieldContext.CurrentScope.Entity);
              break;
            case FieldExecutionType.Resolver:
              result = await InvokeResolverAsync(fieldContext);
              break; 
          }
          var outValue = fieldContext.ConvertToOuputValue(result);
          if (!fieldContext.BatchResultWasSet)
            scope.SetValue(fldIndex, outValue);
          // check 
          var needProcessSelSubset = fieldContext.Flags.IsSet(FieldFlags.ReturnsComplexType) &&
            (result != null || fieldContext.BatchResultWasSet);
          if (needProcessSelSubset)
            _executedObjectFieldContexts.Add(fieldContext);
        } //foreach scope
      }
    } //method


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