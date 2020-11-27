using System;
using NGraphQ.Runtime;

namespace NGraphQL.Core.Scalars {

  public interface IScalarContext {
    void AddError(GraphQLError error, Exception sourceException = null);
  }

}
