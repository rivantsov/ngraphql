using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

  [Flags, Hidden, GraphQLName("__DirectiveLocation")]
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

    [Ignore]
    AllSchemaLocations = ArgumentDefinition | Enum | EnumValue |
         FieldDefinition | InputFieldDefinition | InputObject |
         Interface | Mutation | Object | Scalar,
    [Ignore]
    AllQueryLocations = Query | Mutation | Subscription | Field | FragmentDefinition | FragmentSpread | InlineFragment |
         VariableDefinition

  }

  // these enums are also exposed through introspection, so we use necessary attributes
  // defined in GraphQL spec: https://spec.graphql.org/June2018/#sec-Schema-Introspection

  [Hidden, GraphQLName("__TypeKind")]
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

}
