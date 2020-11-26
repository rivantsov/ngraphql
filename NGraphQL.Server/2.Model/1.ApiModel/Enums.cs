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
    VariableDefinition = 1 << 7, // added in spec v2020
    Schema = 1 << 8,
    Scalar = 1 << 9,
    Object = 1 << 10,
    FieldDefinition = 1 << 11,
    ArgumentDefinition = 1 << 12,
    Interface = 1 << 13,
    Union = 1 << 14,
    Enum = 1 << 15,
    EnumValue = 1 << 16,
    InputObject = 1 << 17,
    InputFieldDefinition = 1 << 18,
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
    ReturnsTask = 1 << 2,
    Hidden = 1 << 3,

    IsBatched = 1 << 4,
    // indicators of arguments in resolver method for the field
    HasParentArg = 1 << 5,
  }

}
