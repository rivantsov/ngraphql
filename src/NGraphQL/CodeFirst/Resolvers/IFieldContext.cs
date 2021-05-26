using System;
using System.Collections.Generic;
using System.Threading;

namespace NGraphQL.CodeFirst {

  public interface IFieldContext {
    ISelectionField SelectionField { get; }
    IRequestContext RequestContext { get; }
    string OperationFieldName { get; }
    CancellationToken CancellationToken { get; }
    IList<object> GetFullRequestPath();
    bool Failed { get; }
    void AddError(GraphQLError error); 

    // to be used by resolver methods, to know in advance which fields  to load from db
    // IList<string> GetAllSelectionSubsetFieldNames();
    
    // Batching (aka DataLoader functionality)
    IList<TEntity> GetAllParentEntities<TEntity>();
    void SetBatchedResults<TEntity, TResult>(IDictionary<TEntity, TResult> results, TResult valueForMissingKeys);
  }

}
