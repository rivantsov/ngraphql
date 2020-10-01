using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model.Construction;

namespace NGraphQL.CodeFirst {

  public abstract class GraphQLTypeRoleAttribute : Attribute { 
    internal abstract ClrTypeRole TypeRole { get; }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class QueryAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.Query; 
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class MutationAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.Mutation;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class SubscriptionAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.Subscription;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ObjectTypeAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.DataType;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class InputTypeAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.DataType;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ResolversAttribute : GraphQLTypeRoleAttribute {
    internal override ClrTypeRole TypeRole => ClrTypeRole.Resolver;
  }

}
