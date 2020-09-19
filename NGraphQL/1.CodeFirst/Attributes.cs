using System;
using NGraphQL.Model;

namespace NGraphQL.CodeFirst {

  /// <summary>Marks the value as nullable. The mapped GraphQL type will not have a not-null marker (!). </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property 
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class NullAttribute: Attribute { }

  /// <summary>Marks a list as possibly containing null values. The mapped GraphQL type will not have a non-null
  /// marker for the element type.  </summary>
  /// <remarks>
  ///   Example: 
  ///     int[] x; -&gt; [Int!]!
  ///     [WithNulls] int[] x; -&gt; [int]!
  /// </remarks>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class WithNullsAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class ScalarAttribute : Attribute {
    public readonly string ScalarName; 
    public ScalarAttribute(string scalarName) {
      ScalarName = scalarName;
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class GraphQLIgnoreAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
                  AttributeTargets.Field | AttributeTargets.Property)]
  public class HiddenAttribute : Attribute { }

  public abstract class ResolverTargetBaseAttribute : Attribute {
    public string FieldName;
    public bool Hidden; // for use by introspection fields like __schema
    public abstract OperationType OperationType { get; }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class QueryAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Query; 
    public QueryAttribute() { }
    public QueryAttribute(string name) { FieldName = name; }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class FieldAttribute : ResolverTargetBaseAttribute {
    public Type OnType;
    public override OperationType OperationType => OperationType.Query;
    public FieldAttribute() { }
    public FieldAttribute(string name) { FieldName = name; }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class MutationAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Mutation;
    public MutationAttribute() { }
    public MutationAttribute(string name) { FieldName = name; }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class SubscriptionAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Subscription;
    public SubscriptionAttribute() { }
    public SubscriptionAttribute(string name) { FieldName = name; }
  }


  public abstract class GraphQLTypeKindBaseAttribute : Attribute {
    public virtual TypeKind Kind { get; set; }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLObjectTypeAttribute : GraphQLTypeKindBaseAttribute {
    public override TypeKind Kind => TypeKind.Object;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLInputAttribute : GraphQLTypeKindBaseAttribute {
    public override TypeKind Kind => TypeKind.InputObject;
  }

  [AttributeUsage(AttributeTargets.Interface)]
  public class GraphQLInterfaceAttribute : GraphQLTypeKindBaseAttribute {
    public override TypeKind Kind => TypeKind.Interface;
  }
  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLUnionAttribute : GraphQLTypeKindBaseAttribute {
    public override TypeKind Kind => TypeKind.Union;
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
                   AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | 
                   AttributeTargets.Parameter)]
  public class GraphQLNameAttribute : Attribute {
    public string Name; 
    public GraphQLNameAttribute(string name) {
      Name = name; 
    }
  }

}
