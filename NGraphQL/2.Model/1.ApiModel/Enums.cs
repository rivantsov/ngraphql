using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {

  public enum OperationType {
    Query,
    Mutation,
    Subscription
  }

  // these enums are also exposed through introspection, so we use necessary attributes
  // defined in GraphQL spec: https://spec.graphql.org/June2018/#sec-Schema-Introspection

  [GraphQLName("__TypeKind"), Hidden] //name is for use in introspection classes
  public enum TypeKind {
    Scalar,
    Object,
    Interface,
    Union,
    Enum,
    InputObject,
    List,
    NotNull,
  }

  [Flags, GraphQLName("__DirectiveLocation"), Hidden]
  public enum DirectiveLocation {
    [Ignore] //the value should not appear in any output, used internally only
    None = 0,

    Query = 1,
    Mutation = 1 << 1,
    Subscription = 1 << 2,
    Field = 1 << 3,
    FragmentDefinition = 1 << 4,
    FragmentSpread = 1 << 5,
    InlineFragment = 1 << 6,
    Schema = 1 << 7,
    Scalar = 1 << 8,
    Object = 1 << 9,
    FieldDefinition = 1 << 10,
    ArgumentDefinition = 1 << 11,
    Interface = 1 << 12,
    Union = 1 << 13,
    Enum = 1 << 14,
    EnumValue = 1 << 15,
    InputObject = 1 << 16,
    InputFieldDefinition = 1 << 17,
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
    IsBatched = 1 << 4,
    ReturnsTask = 1 << 5,
    // indicators of arguments in resolver method for the field
    HasParentArg = 1 << 6,
    Hidden = 1 << 7,
  }

}
