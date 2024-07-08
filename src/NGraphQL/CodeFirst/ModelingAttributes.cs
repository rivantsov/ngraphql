using System;

namespace NGraphQL.CodeFirst {


  /// <summary>Specifies the name of GraphQL schema element (type or field) explicitly. </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
                   AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method |
                   AttributeTargets.Parameter)]
  public class GraphQLNameAttribute : Attribute {
    public string Name;
    public GraphQLNameAttribute(string name) {
      Name = name;
    }
  }

  /// <summary>Marks the value as nullable. The mapped GraphQL type will not have a non-null marker (!). </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property 
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class NullAttribute: Attribute { }

  /// <summary>Marks the value as not nullable.</summary>
  /// <remarks>Opposite of [Null] attribute. Use it with GraphQLServerOptions.RefTypesNullableByDefault flag that
  /// makes all reference types nullable by default. </remarks>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class NotNullAttribute : Attribute { }

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


}
/// <summary> Specifies list of interfaces implemented by an interface or Object type. </summary>
/// <remarks> One way to specify that GraphQL type implements an interface is to simply make the .NET type for Object type
/// implement the corresponding interface. However, c# requires that exact match (member names, types) between
/// interface and implementing type. GraphQL has weaker requirement, types can be covariant. 
/// 
/// </remarks>
[AttributeUsage(AttributeTargets.Class |AttributeTargets.Interface)]
public class ImplementsAttribute : Attribute {
  public Type[] Types;
  public ImplementsAttribute(params Type[] types) {
    Types = types;
  }
}

