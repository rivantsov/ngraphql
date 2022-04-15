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
    ISelectionField IFieldContext.SelectionField => this.MappedField.Field;
    public IRequestContext RequestContext => _requestContext;
    public CancellationToken CancellationToken => _requestContext.CancellationToken;
    public string OperationFieldName => _executer.OperationFieldName;
    public bool Failed => _executer.Failed; 
    // GetFullRequestPath, AddError - see below


    internal OutputObjectScope CurrentParentScope => _currentParentScope;

   // internal SelectionField SelectionField => MappedField.Field;
    internal readonly MappedSelectionField MappedField;
    internal readonly FieldDef FieldDef;
    internal readonly TypeDefBase TypeDef;
    internal SourceLocation SourceLocation => MappedField.Field.SourceLocation;

    internal object[] ArgValues = null;
    internal object ResolverClassInstance;
    internal IList<OutputObjectScope> AllParentScopes = OutputObjectScope.EmptyList;
    internal IList<OutputObjectScope> AllResultScopes = OutputObjectScope.EmptyList;
    internal bool BatchResultWasSet;
    OperationFieldExecuter _executer { get; }

    public bool Skip;
    public string Format;

    RequestContext _requestContext;
    OutputObjectScope _currentParentScope;

    public FieldContext(RequestContext requestContext, OperationFieldExecuter fieldExecuter, MappedSelectionField mappedField,
                         IList<OutputObjectScope> allParentScopes = null) {
      _requestContext = requestContext;
      _executer = fieldExecuter;
      MappedField = mappedField;
      FieldDef = MappedField.Resolver.Field;
      TypeDef = FieldDef.TypeRef.TypeDef;
      AllParentScopes = allParentScopes ?? OutputObjectScope.EmptyList;
      if (MappedField.Field.SelectionSubset != null)
        AllResultScopes = new List<OutputObjectScope>();
    }

    public override string ToString() => MappedField.ToString();

    public void AddError(GraphQLError error) {
      _executer.AddError(error); 
    }

    internal void SetCurrentParentScope(OutputObjectScope scope) {
      _currentParentScope = scope;
    }

    public object ConvertToOutputValue(object result) {
      // validate result value
      if (this.FieldDef.Flags.IsSet(FieldFlags.ReturnsComplexType)) {
        var rank = this.FieldDef.TypeRef.Rank;
        var path = this.CurrentParentScope.Path.Append(this.MappedField.Field.Key);
        return CreateObjectFieldResultScopes(result, rank, path);
      }
      // cover conversions like enums to strings
      var outValue = TypeDef.ToOutput(this, result);
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
      // special case - Union
      if (TypeDef.Kind == TypeKind.Union) {
        if (entity is UnionBase ub)
          entity = ub.Value;
        if (entity == null)
          return null;
      }
      var entType = entity.GetType(); 
      var mapping = TypeDef.FindMapping(entType);
      if (mapping == null)
        throw new FatalServerException($"FATAL: failed to find mapping for entity type {entType} in the type {TypeDef.Name}. ");
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
      foreach (var scope in this.AllParentScopes) {
        if (!results.TryGetValue((TEntity)scope.Entity, out var result))
          result = valueForMissingKeys;
        var outValue = this.ConvertToOutputValue(result);
        scope.AddValue(this.MappedField.Field.Key, outValue);
      }
      this.BatchResultWasSet = true;
    }

  }
}
