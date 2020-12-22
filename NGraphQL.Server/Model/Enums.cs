using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {


  public enum ObjectTypeRole {
    // all types defining data (Object type, input type, enum, etc)
    Data,
    // special types registered at module level
    ModuleQuery,
    ModuleMutation,
    ModuleSubscription,
    // final root Query, Mutation, Subscr, Schema objects constructed for entire app/api
    Schema,
  }


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
