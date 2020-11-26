using System;

namespace NGraphQL.CodeFirst {


  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
                   AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method |
                   AttributeTargets.Parameter)]
  public class GraphQLNameAttribute : Attribute {
    public string Name;
    public GraphQLNameAttribute(string name) {
      Name = name;
    }
  }

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

  /// <summary>Identifies the name of the Scalar to use as a type for the member. </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class ScalarAttribute : Attribute {
    public readonly string ScalarName; 
    public ScalarAttribute(string scalarName) {
      ScalarName = scalarName;
    }
  }

  /// <summary>Instructs the engine to completely ignore this member on .NET type. </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
  public class IgnoreAttribute : Attribute { }

  /// <summary>Marks element as hidden, not showing up in the Schema document. </summary>
  /// <remarks>All introspection types and hidden field __typename use this attribute.</remarks>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
                  AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
  public class HiddenAttribute : Attribute { }


  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
  public class ResolverAttribute : Attribute {
    public Type ResolverClass;
    public string MethodName;
    public ResolverAttribute(string methodName, Type resolverClass = null) {
      MethodName = methodName;
      ResolverClass = resolverClass; 
    }
  }

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : Attribute {
    public readonly string Reason;
    public DeprecatedDirAttribute(string reason) {
      Reason = reason;
    }
  }

}
