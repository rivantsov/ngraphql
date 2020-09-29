using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model.Construction;

namespace NGraphQL.CodeFirst {

  public abstract class GraphQLTypeCategoryAttribute : Attribute { 
    internal abstract RegisteredTypeCategory Category { get; }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class QueryAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.Query; 
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class MutationAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.Mutation;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class SubscriptionAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.Subscription;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ObjectTypeAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.DataType;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class InputTypeAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.DataType;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ResolversAttribute : GraphQLTypeCategoryAttribute {
    internal override RegisteredTypeCategory Category => RegisteredTypeCategory.Resolver;
  }

}
