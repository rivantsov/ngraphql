using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public partial class FieldContext : IFieldContext {
    // IFieldContext members
    ISelectionField IFieldContext.SelectionField => this.SelectionField;
    public IRequestContext RequestContext => _requestContext;
    public IOperationFieldContext RootField { get; }
    public CancellationToken CancellationToken => _requestContext.CancellationToken;
    // GetFullRequestPath - see below


    internal OutputObjectScope CurrentParentScope => _currentParentScope;
    internal FieldResolverInfo CurrentResolver => _currentResolver;
    internal FieldDef CurrentFieldDef => _currentResolver?.Field;

    internal SelectionField SelectionField;

    internal object[] ArgValues = null;
    internal object ResolverClassInstance;
    internal IList<OutputObjectScope> AllParentScopes = OutputObjectScope.EmptyList;
    internal IList<OutputObjectScope> AllResultScopes = OutputObjectScope.EmptyList;
    internal bool BatchResultWasSet;

    public bool Skip;
    public string Format;

    RequestContext _requestContext;
    OutputObjectScope _currentParentScope;
    FieldResolverInfo _currentResolver; 

    public FieldContext(RequestContext requestContext, IOperationFieldContext rootField, SelectionField selField,
                         IList<OutputObjectScope> allParentScopes = null) {
      _requestContext = requestContext;
      RootField = rootField;
      this.SelectionField = selField;
      AllParentScopes = allParentScopes ?? OutputObjectScope.EmptyList;
      if (selField.SelectionSubset != null)
        AllResultScopes = new List<OutputObjectScope>();
    }

    public override string ToString() => SelectionField.ToString();

    internal void SetCurrentParentScope(OutputObjectScope scope) {
      // Root scope has no entity!
      var typeChangeOrNull = scope.Entity == null || _currentParentScope?.Entity.GetType() != scope.Entity.GetType(); 
      _currentParentScope = scope;
      if (typeChangeOrNull)
        SetupResolver();
    }

    // Called to setup resolver after new _currentParentScope was set. 
    // the keyword here is efficiency, we do not want to search for resolver if previous one is OK (prev for prev scope/entity type)
    private void SetupResolver() {
      // Try default resolver in SelectionField
      var sc = _currentParentScope;
      var scopeTypeDef = sc.Mapping.TypeDef;
      var defaultRes = this.SelectionField.DefaultResolver;
      if (defaultRes != null) {
        if (defaultRes.TypeMapping == sc.Mapping) {
          _currentResolver = defaultRes;
          return; 
        }
      }
      // find it in type mapping for current scope 
      _currentResolver = sc.Mapping.FieldResolvers
        .FirstOrDefault(fr => fr.Field.Name == this.SelectionField.Name);
      if (_currentResolver == null)
        throw new FatalServerException(
          $"Failed to find resolver for field {SelectionField.Name}, target type: {sc.Mapping.TypeDef.Name}, entity type: {sc.Entity.GetType()}");
      // set as default resolver in sel field if not set
      SelectionField.DefaultResolver ??= _currentResolver;
    }

    public object ConvertToOuputValue(object result) {
      // validate result value
      var fldDef = _currentResolver.Field; 
      if (fldDef.Flags.IsSet(FieldFlags.ReturnsComplexType)) {
        var rank = fldDef.TypeRef.Rank;
        var path = this.CurrentParentScope.Path.Append(this.SelectionField.Key);
        return CreateObjectFieldResultScopes(result, rank, path);
      }
      // cover conversions like enums to strings
      var typeDef = fldDef.TypeRef.TypeDef;
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
      var typeDef = _currentResolver.Field.TypeRef.TypeDef;
      // special case - Union
      if (typeDef.Kind == TypeKind.Union) {
        if (entity is UnionBase ub)
          entity = ub.Value;
        if (entity == null)
          return null;
      }

      var mapping = typeDef.FindMapping(entity.GetType());
      var scope = new OutputObjectScope(path, entity, mapping);
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
      fullPath.Add(this.SelectionField.Key);
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
        scope.SetValue(this.SelectionField.Key, outValue);
      }
      this.BatchResultWasSet = true;
    }

  }
}
