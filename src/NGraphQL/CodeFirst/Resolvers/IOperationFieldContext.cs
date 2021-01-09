using System;
using NGraphQL.Server;

namespace NGraphQL.CodeFirst {

  public interface IOperationFieldContext {
    string OperationFieldName { get; }
    void AddError(GraphQLError error);
    bool Failed { get; }
  }
}
