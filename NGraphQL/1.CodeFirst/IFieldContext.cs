using System;
using System.Collections.Generic;
using System.Threading;

using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.CodeFirst {

  public interface IFieldContext {
    SelectionField SelectionField { get; }
    FieldDef FieldDef { get; }
    IRequestContext RequestContext { get; }
    IOperationFieldContext RootField { get; }
    CancellationToken CancellationToken { get; }
    GraphQLApiModel GetModel();
    IList<object> GetFullRequestPath();
    // to be used by resolver methods, to know in advance which fields  to load from db
    IList<string> GetAllSelectionSubsetFieldNames();
    // Batching (aka DataLoader functionality)
    IList<TEntity> GetAllParentEntities<TEntity>();
    void SetBatchedResults<TEntity, TResult>(IDictionary<TEntity, TResult> results);
  }

}
