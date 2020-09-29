using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  public abstract class GraphQLTypeAttributeBase : Attribute { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class QueryAttribute : GraphQLTypeAttributeBase { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class MutationAttribute : GraphQLTypeAttributeBase { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class SubscriptionAttribute : GraphQLTypeAttributeBase { }


  [AttributeUsage(AttributeTargets.Class)]
  public class ObjectTypeAttribute : GraphQLTypeAttributeBase {
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class InputTypeAttribute : GraphQLTypeAttributeBase {
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class ResolversAttribute : GraphQLTypeAttributeBase {
  }



}
