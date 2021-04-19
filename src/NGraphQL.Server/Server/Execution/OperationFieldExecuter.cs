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
    List<FieldContext> _executedObjectFieldContexts = new List<FieldContext>();

    public OperationFieldExecuter(RequestContext requestContext, MappedSelectionField opField, OutputObjectScope parentScope) {
      _requestContext = requestContext;
      _parentScope = parentScope;
      _operationField = opField;
    }

    public async Task ExecuteOperationFieldAsync() {
      try {
        var opFieldContext = new FieldContext(_requestContext, this, _operationField);
        opFieldContext.CurrentScope = _parentScope;
        var result = await InvokeResolverAsync(opFieldContext);
        var opOutValue = opFieldContext.ConvertToOuputValue(result);
        // Safe means with lock, protect concurrent access
        _parentScope.SetValueSafe(_operationField.Field.Key, opOutValue);
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
      // all scopes have sc.Entity field != null
      var scopes = fieldContext.AllResultScopes;
      var outTypeDef = fieldContext.FieldDef.TypeRef.TypeDef;
      var selSubSet = fieldContext.Field.Field.SelectionSubset;
      switch(outTypeDef.Kind) {
        case TypeKind.Object:
          await ExecuteObjectsSelectionSubsetAsync(fieldContext.Field, scopes, selSubSet);
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
            await ExecuteObjectsSelectionSubsetAsync(grp.ToList(), grp.Key, selSubSet);
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
        if (mappedItem.HasDirectives) {
          ApplyDirectives(mappedItem, parentScopes, out var skip);
          if (skip)
            continue;
        }
        // if it is a fragment spread, make recursive call to process fragment fields
        if (mappedItem is MappedFragmentSpread mappedFragm) {
          await ExecuteObjectsSelectionSubsetAsync(parentField, parentScopes, mappedFragm.Spread.Fragment.SelectionSubset);
          continue; 
        }

        // It is a plain field
        var mappedField = (MappedSelectionField)mappedItem;
        var fldDef = mappedField.FieldDef;

        var returnsComplexType = fldDef.Flags.IsSet(FieldFlags.ReturnsComplexType);
        var fieldContext = new FieldContext(_requestContext, this, mappedField, parentScopes);
        // Process each scope for the field
        foreach (var scope in parentScopes) {
          if (fieldContext.BatchResultWasSet && scope.HasValue(mappedField.Field.Key))
            continue; 
          fieldContext.CurrentScope = scope;
          object result = null;
          // special case, when parent is Gql type, not entity; in this case just read its property
          if (scope.EntityIsGqlType) {
            result = ReadGraphQLObjectValue(fldDef, scope.Entity);
          } else { 
            switch (mappedField.Resolver.ResolverKind) {
              case ResolverKind.CompiledExpression:
                result = InvokeFieldReader(fieldContext, fieldContext.CurrentScope.Entity);
                break;
              case ResolverKind.Method:
                result = await InvokeResolverAsync(fieldContext);
                break;
            }
          } // else
          if (!fieldContext.BatchResultWasSet && result != null) {
            var outValue = fieldContext.ConvertToOuputValue(result);
            scope.SetValue(fldIndex, outValue);
          }
        } //foreach scope
        // if there are any non-null object-type results, add this field context to this special list
        //   to execute selection subsets in the next round. 
        if (returnsComplexType && fieldContext.AllResultScopes.Count > 0) {
          _executedObjectFieldContexts.Add(fieldContext);
        }
      } //foreach fldIndex
    } //method

    private void ApplyDirectives(MappedSelectionItem mappedItem, IList<OutputObjectScope> scopes, out bool skip) {
      skip = true; 
    }
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