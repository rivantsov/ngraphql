using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  public partial class SelectionItemContext : IFieldContext {
    public readonly MappedSelectionItem MappedItem;
    public MappedSelectionField MappedField => (MappedSelectionField)MappedItem;
    public FieldFlags Flags => MappedField.FieldDef.Flags;

    // IFieldContext members
    public IRequestContext RequestContext => _requestContext;
    public GraphQLApiModel GetModel() => _requestContext.ApiModel;
    public ISelectionField SelectionField => MappedField.Field;
    public FieldDef FieldDef => MappedField?.FieldDef;
    public CancellationToken CancellationToken => _requestContext.CancellationToken;
    public IOperationFieldContext RootField { get; }
    public SourceLocation SourceLocation => MappedItem.Item.SourceLocation;

    internal object[] ArgValues = null;
    internal object ResolverClassInstance;
    internal OutputObjectScope CurrentScope;
    internal IList<OutputObjectScope> AllParentScopes = OutputObjectScope.EmptyList;
    internal IList<OutputObjectScope> AllResultScopes = OutputObjectScope.EmptyList;
    internal bool BatchResultWasSet;

    public bool Skip;
    public string Format;

    RequestContext _requestContext;

    public SelectionItemContext(RequestContext requestContext, IOperationFieldContext rootField, MappedSelectionItem item,
                         IList<OutputObjectScope> allParentScopes = null) {
      _requestContext = requestContext;
      RootField = rootField;
      MappedItem = item;
      AllParentScopes = allParentScopes ?? OutputObjectScope.EmptyList;
      if (Flags.IsSet(FieldFlags.ReturnsComplexType))
        AllResultScopes = new List<OutputObjectScope>();
    }

    public override string ToString() => MappedField.ToString();


    public object ConvertToOuputValue(object result) {
      // validate result value
      if (Flags.IsSet(FieldFlags.ReturnsComplexType)) {
        var rank = this.MappedField.FieldDef.TypeRef.Rank;
        var path = this.CurrentScope.Path.Append(this.MappedField.Field.Key);
        return CreateObjectFieldResultScopes(result, rank, path);
      }
      // cover conversions like enums to strings
      var typeDef = MappedField.FieldDef.TypeRef.TypeDef;
      var outValue = typeDef.ToOutput(this, result);
      return outValue;
    }

    public object CreateObjectFieldResultScopes(object rawResult, int rank, RequestPath path) {
      if (rawResult == null)
        return null;
      // check field depth against quota
      if (path.FieldDepth > _requestContext.Quota.MaxDepth)
        this.ThrowFieldDepthExceededQuota();
      if (rank > 0)
        return CreateObjectScopeList(rawResult, rank, path);
      else
        return CreateObjectFieldResultScope(rawResult, path);
    }

    public object CreateObjectFieldResultScope(object rawResult, RequestPath path) {
      var typeDef = MappedField.FieldDef.TypeRef.TypeDef;
      // special case - Union
      if (typeDef.Kind == TypeKind.Union) {
        if (rawResult is UnionBase ub)
          rawResult = ub.Value;
        if (rawResult == null)
          return null;
      }
      //this.Field.FieldDef.Flags.IsSet(FieldFlags.ResolverReturnsGraphQLObject);

      var scope = new OutputObjectScope(this.MappedField, path, rawResult);
      AllResultScopes.Add(scope);
      var newCount = Interlocked.Increment(ref _requestContext.Metrics.OutputObjectCount);
      // check total count against quota
      if (newCount > _requestContext.Quota.MaxOutputObjects)
        this.ThrowObjectCountExceededQuota();
      return scope;
    }

    private IList<object> CreateObjectScopeList(object rawResult, int rank, RequestPath path) {
      var list = (IEnumerable)rawResult;
      var scopes = new List<object>();
      var index = 0;
      foreach (var item in list) {
        var itemScope = CreateObjectFieldResultScopes(item, rank - 1, path.Append(index++));
        scopes.Add(itemScope);
      }
      return scopes;
    }

    public IList<object> GetFullRequestPath() {
      var fullPath = CurrentScope.Path.GetFullPath();
      fullPath.Add(this.MappedField.Field.Key);
      return fullPath; 
    }

    public IList<TEntity> GetAllParentEntities<TEntity>() {
      if (this.AllParentScopes.Count == 0) //the case for top-level field/scope
        return new TEntity[] { };
      return this.AllParentScopes.Where(s => s.Entity != null && s.Entity is TEntity)
                            .Select(s => (TEntity)s.Entity).ToList();
    }

    public void SetBatchedResults<TEntity, TResult>(IDictionary<TEntity, TResult> results, TResult valueForMissingKeys) {
      // TODO: add validation of types: TEntity -> typeof(Entity), TResult==fieldType
      /*
      var returnsObj = this.Flags.IsSet(FieldFlags.ReturnsComplexType);
      var fldType = this.Field.FieldDef.TypeRef.TypeDef.ClrType; 
      if (!fldType.IsAssignableFrom(typeof(TResult))) {
        throw new ResolverException($"Resolver error: SetBatchResults is called with arg of invalid type. " + 
                                    $"Expected dictionary with values of type {fldType}", 
                                    this.GetCodePath());
      }
      */
      foreach (var scope in this.AllParentScopes) {
        if (!results.TryGetValue((TEntity)scope.Entity, out var result))
          result = valueForMissingKeys;
        var outValue = this.ConvertToOuputValue(result);
        scope.SetValue(this.FieldIndex, outValue);
      }
      this.BatchResultWasSet = true;
    }

  }
}
