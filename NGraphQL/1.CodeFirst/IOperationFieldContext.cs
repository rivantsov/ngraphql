using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Server;

namespace NGraphQL.CodeFirst {
  public interface IOperationFieldContext {
    string OperationFieldName { get; }
    void AddError(GraphQLError error);
    bool Failed { get; }
  }
}
