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
    // IFieldContext members
    public IRequestContext RequestContext => _requestContext;
    public GraphQLApiModel GetModel() => _requestContext.ApiModel;
    public ISelectionField SelectionField => this.SelField;
    public FieldDef FieldDef => CurrentFieldDef;
    public CancellationToken CancellationToken => _requestContext.CancellationToken;
    public IOperationFieldContext RootField { get; }
    public SourceLocation SourceLocation => Item.SourceLocation;

    public readonly SelectionItem Item;
    public SelectionField SelField => (SelectionField)Item;
    public ObjectTypeMappingExt ParentMapping; 

    internal object[] ArgValues = null;
    internal object ResolverClassInstance;
    internal OutputObjectScope CurrentParentScope => _currentParentScope;
    internal FieldResolverInfo CurrentResolver => _currentResolver;
    internal FieldDef CurrentFieldDef => _currentResolver?.Field; 
    internal IList<OutputObjectScope> AllParentScopes = OutputObjectScope.EmptyList;
    internal IList<OutputObjectScope> AllResultScopes = OutputObjectScope.EmptyList;
    internal bool BatchResultWasSet;

    public bool Skip;
    public string Format;

    RequestContext _requestContext;
    OutputObjectScope _currentParentScope;
    FieldResolverInfo _currentResolver; 

    public SelectionItemContext(RequestContext requestContext, IOperationFieldContext rootField, SelectionItem item,
                         IList<OutputObjectScope> allParentScopes = null) {
      _requestContext = requestContext;
      RootField = rootField;
      Item = item;
      AllParentScopes = allParentScopes ?? OutputObjectScope.EmptyList;
      if (Flags.IsSet(FieldFlags.ReturnsComplexType))
        AllResultScopes = new List<OutputObjectScope>();
    }

    public override string ToString() => SelectionField.ToString();

    internal void SetCurrentParentScope(OutputObjectScope scope) {
      var typeChange = _currentParentScope?.Entity.GetType() != scope.Entity.GetType(); 
      _currentParentScope = scope;
      if (typeChange)
        SetupResolver();
    }

    private void SetupResolver() {
      var field = SelField;
      _currentResolver = field.DefaultResolver;
      _currentResolver = _currentParentScope.Mapping.FieldResolvers
        .FirstOrDefault(fr => fr.Field.Name == this.SelectionField.Name);

    }

    public object ConvertToOuputValue(object result) {
      // validate result value
      if (Flags.IsSet(FieldFlags.ReturnsComplexType)) {
        var rank = this.SelectionField.FieldDef.TypeRef.Rank;
        var path = this.CurrentParentScope.Path.Append(this.SelectionField.Field.Key);
        return CreateObjectFieldResultScopes(result, rank, path);
      }
      // cover conversions like enums to strings
      var typeDef = _fieldDef. Field.FieldDef.TypeRef.TypeDef;
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

    public object CreateObjectFieldResultScope(object entity, RequestPath path) {
      var typeDef = _fieldDef.TypeRef.TypeDef;
      // special case - Union
      if (typeDef.Kind == TypeKind.Union) {
        if (entity is UnionBase ub)
          entity = ub.Value;
        if (entity == null)
          return null;
      }

      var mapping = typeDef.FindMapping(entity.GetType());
      var scope = new OutputObjectScope(this.SelectionField, path, entity, mapping);
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
      var fullPath = CurrentParentScope.Path.GetFullPath();
      fullPath.Add(this.SelectionField.Field.Key);
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
        scope.SetValue(this.SelectionField.Field.Key, outValue);
      }
      this.BatchResultWasSet = true;
    }

  }
}
