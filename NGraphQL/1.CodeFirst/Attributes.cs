using System;
using System.Net.NetworkInformation;
using NGraphQL.Model;

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

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class ScalarAttribute : Attribute {
    public readonly string ScalarName; 
    public ScalarAttribute(string scalarName) {
      ScalarName = scalarName;
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class IgnoreAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
                  AttributeTargets.Field | AttributeTargets.Property)]
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

  /*
  [AttributeUsage(AttributeTargets.Method)]
  public class FieldAttribute : ResolverTargetBaseAttribute {
    public Type OnType;
    public override OperationType OperationType => OperationType.Query;
    public FieldAttribute() { }
    public FieldAttribute(string name) { FieldName = name; }
  }
  */


}
