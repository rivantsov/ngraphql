using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {

  public enum OperationType {
    Query,
    Mutation,
    Subscription
  }

  public enum FieldExecutionType {
    NotSet,
    Reader,
    Resolver
  }

  [Flags]
  public enum FieldFlags {
    None = 0,
    Nullable = 1,
    ReturnsComplexType = 1 << 1,
    ReturnsTask = 1 << 2,
    Hidden = 1 << 3,

    IsBatched = 1 << 4,
    // indicators of arguments in resolver method for the field
    HasParentArg = 1 << 5,
  }

}
