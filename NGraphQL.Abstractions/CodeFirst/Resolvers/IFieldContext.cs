using System;
using System.Collections.Generic;
using System.Threading;
using NGraphQL.Core;

namespace NGraphQL.CodeFirst {

  public interface IFieldContext {
    ISelectionField SelectionField { get; }
    IRequestContext RequestContext { get; }
    IOperationFieldContext RootField { get; }
    CancellationToken CancellationToken { get; }
    IList<object> GetFullRequestPath();
    IList<Directive> Directives { get; }

    // to be used by resolver methods, to know in advance which fields  to load from db
    IList<string> GetAllSelectionSubsetFieldNames();
    
    // Batching (aka DataLoader functionality)
    IList<TEntity> GetAllParentEntities<TEntity>();
    void SetBatchedResults<TEntity, TResult>(IDictionary<TEntity, TResult> results);
  }

}
