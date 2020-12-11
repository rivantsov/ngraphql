using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  /// <summary>Specifies the target field of ObjectType for a resolver method. This attribute is the opposite 
  /// of the Resolver attibute, which is put on object field and specifies the resolver method. </summary>
  /// <remarks>You can use either this attribute or Resolver attribute to link the field and its resolver. 
  /// We encourage you to use Resolves attibute (this one) to keep clear the object types from resolver 
  /// references (which are server-side classes), so that these types can be used in strongly-typed clients. 
  /// </remarks>
  [AttributeUsage(AttributeTargets.Method)]
  public class ResolvesAttribute : Attribute {
    public Type TargetType;
    public string FieldName;
    public ResolvesAttribute(string fieldName, Type targetType = null) {
      FieldName = fieldName;
      TargetType = targetType;
    }
  }

  /// <summary>Specifies the resolver method for a field of an Object GraphQL type. We encourage to use 
  /// the Resolves attribute instead of this attribute. </summary>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
  public class ResolverAttribute : Attribute {
    public Type ResolverClass;
    public string MethodName;
    public ResolverAttribute(string methodName, Type resolverClass = null) {
      MethodName = methodName;
      ResolverClass = resolverClass;
    }
  }


}
