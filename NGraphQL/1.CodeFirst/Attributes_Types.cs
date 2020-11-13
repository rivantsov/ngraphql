using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Model.Construction;

namespace NGraphQL.CodeFirst {

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class QueryAttribute : GraphQLTypeRoleAttribute {
    internal override SchemaTypeRole TypeRole => SchemaTypeRole.Query; 
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class MutationAttribute : GraphQLTypeRoleAttribute {
    internal override SchemaTypeRole TypeRole => SchemaTypeRole.Mutation;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class SubscriptionAttribute : GraphQLTypeRoleAttribute {
    internal override SchemaTypeRole TypeRole => SchemaTypeRole.Subscription;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ObjectTypeAttribute : GraphQLTypeRoleAttribute {
    internal override SchemaTypeRole TypeRole => SchemaTypeRole.DataType;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class InputTypeAttribute : GraphQLTypeRoleAttribute {
    internal override SchemaTypeRole TypeRole => SchemaTypeRole.DataType;
  }

}
