using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.RequestModel;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public partial class FieldContext : IFieldContext {
    // IFieldContext
    public GraphQLApiModel GetModel() => _requestContext.ApiModel;
    public ISelectionField SelectionField => Field.SelectionField;
    public FieldDef FieldDef => Field?.FieldDef;
    public CancellationToken CancellationToken => _requestContext.CancellationToken;
    public IOperationFieldContext RootField { get; }
    public IRequestContext RequestContext => _requestContext;

    public readonly MappedField Field;
    public readonly int FieldIndex;
    public FieldFlags Flags => Field.FieldDef.Flags;

    internal object[] ArgValues = null;
    internal object ResolverClassInstance;
    internal OutputObjectScope CurrentScope;
    internal IList<OutputObjectScope> AllParentScopes = OutputObjectScope.EmptyList;
    internal IList<OutputObjectScope> AllResultScopes = OutputObjectScope.EmptyList;
    internal bool BatchResultWasSet;

    RequestContext _requestContext;

    public FieldContext(RequestContext requestContext, IOperationFieldContext rootField, MappedField field, int fieldIndex,
                         IList<OutputObjectScope> allParentScopes = null) {
      _requestContext = requestContext;
      RootField = rootField;
      Field = field;
      FieldIndex = fieldIndex;
      AllParentScopes = allParentScopes ?? OutputObjectScope.EmptyList;
      if (Flags.IsSet(FieldFlags.ReturnsComplexType))
        AllResultScopes = new List<OutputObjectScope>();
    }

    public IList<Directive> GetDirectives() {
      if (this.SelectionField.Directives.Count == 0 && this.FieldDef.Directives.Count == 0)
        return Directive.EmptyList;
      return Field.SelectionField.Directives.Union(FieldDef.Directives).ToList(); 
    }

    public override string ToString() => Field.ToString();

    private static string[] _emptyStrings = new string[] { }; 

    public IList<string> GetAllSelectionSubsetFieldNames() {
      if (!Flags.IsSet(FieldFlags.ReturnsComplexType))
        return _emptyStrings;
      var subSet = this.Field.SelectionField.SelectionSubset;
      // if there is a single mapped set (most common case), return its field list
      if (subSet.MappedFieldSets.Count == 1)
        return subSet.MappedFieldSets[0].GetFieldNames().ToList();
      // We have more than one possible actual type (owner field returns union or interface)
      // merge multiple field lists into a single set
      var allFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var mappedSet in subSet.MappedFieldSets)
        allFields.UnionWith(mappedSet.GetFieldNames());
      return allFields.ToList(); 
    }

    public object ConvertToOuputValue(object result) {
      // validate result value
      //ValidateFieldResult(field, resultValue);
      if (Flags.IsSet(FieldFlags.ReturnsComplexType)) {
        var rank = this.Field.FieldDef.TypeRef.Rank;
        var path = this.CurrentScope.Path.Append(this.Field.SelectionField.Key);
        return CreateObjectFieldResultScopes(result, rank, path);
      }
      // cover conversions like enums to strings
      var typeDef = Field.FieldDef.TypeRef.TypeDef;
      var outValue = typeDef.ToOutput(this, result);
      return outValue;
    }

    public object CreateObjectFieldResultScopes(object rawResult, int rank, RequestPath path) {
      if (rawResult == null)
        return null;
      
      // check field depth against quota
      if (path.FieldDepth > _requestContext.Quota.MaxDepth)
        this.ThrowFieldDepthExceededQuota();

      switch (rank) {
        case 0:
          var typeDef = Field.FieldDef.TypeRef.TypeDef;
          // special cases - Union, Interface; extract actual value from box
          switch (typeDef.Kind) {
            case TypeKind.Union:
              if (rawResult is UnionBase ub)
                rawResult = ub.Value;
              if (rawResult == null)
                return null;
              break;

            case TypeKind.Interface:
              if (rawResult == null)
                return null;
              break;
          }
          var scope = new OutputObjectScope(this, path, rawResult);
          AllResultScopes.Add(scope);
          var newCount = Interlocked.Increment(ref _requestContext.Metrics.OutputObjectCount);
          // check total count against quota
          if (newCount > _requestContext.Quota.MaxOutputObjects)
            this.ThrowObjectCountExceededQuota();
          return scope;

        default: // rank > 0, array
          var list = rawResult as IList;
          var scopes = new object[list.Count];
          for (int i = 0; i < list.Count; i++) {
            scopes[i] = CreateObjectFieldResultScopes(list[i], rank - 1, path.Append(i));
          }
          return scopes;
      }
    }

    public IList<object> GetFullRequestPath() {
      var fullPath = CurrentScope.Path.GetFullPath();
      fullPath.Add(this.Field.SelectionField.Key);
      return fullPath; 
    }

    public IList<TEntity> GetAllParentEntities<TEntity>() {
      if (this.AllParentScopes.Count == 0) //the case for top-level field/scope
        return new TEntity[] { };
      return this.AllParentScopes.Where(s => s.Entity != null && s.Entity is TEntity)
                            .Select(s => (TEntity)s.Entity).ToList();
    }

    public void SetBatchedResults<TEntity, TResult>(IDictionary<TEntity, TResult> results) {
      // TODO: add validation of types: TEntity -> typeof(Entity), TResult==fieldType
      /*
      var fldType = this.Field.FieldDef.TypeRef.TypeDef.ClrType; 
      if (!fldType.IsAssignableFrom(typeof(TResult))) {
        throw new ResolverException($"Resolver error: SetBatchResults is called with arg of invalid type. " + 
                                    $"Expected dictionary with values of type {fldType}", 
                                    this.GetCodePath());
      }
      */
      //BatchedResultsLookup = results.ToDictionary(kv => (object) kv.Key, kv => (object) kv.Value);
      var returnsObj = this.Flags.IsSet(FieldFlags.ReturnsComplexType);
      foreach (var scope in this.AllParentScopes) {
        if (results.TryGetValue((TEntity)scope.Entity, out var result)) {
          var outValue = this.ConvertToOuputValue(result);
          scope.SetValue(this.FieldIndex, outValue);
        }
      }
      this.BatchResultWasSet = true;
    }

  }
}
