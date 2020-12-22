using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {


  public enum ObjectTypeRole {
    // all types defining data (Object type, input type, enum, etc)
    Data,
    // Query, Mutation, Subscription types registered at module level; their role is to define fields that will end up corresponding 
    //  root types for the entire API. So final schema Query type will include all fields from Query types defined at module level. 
    ModuleQuery,
    ModuleMutation,
    ModuleSubscription,

    // Root schema objects constructed for the entire app/api
    Query,
    Mutation,
    Subscription,
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
