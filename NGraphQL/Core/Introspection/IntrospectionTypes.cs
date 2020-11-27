using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core.Introspection {

  [Flags, Hidden]
  public enum __DirectiveLocation {
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

  [Hidden] //name is for use in introspection classes
  public enum __TypeKind {
    Scalar,
    Object,
    Interface,
    Union,
    Enum,
    InputObject,
    List,
    NotNull,
  }

  [Hidden]
  public class __Schema {
    public IList<__Type> Types = new List<__Type>();
    public __Type QueryType;
    public __Type MutationType;
    public __Type SubscriptionType;
    public IList<__Directive> Directives = new List<__Directive>();
  }

  public abstract class IntroObjectBase {
    public string Name;
    [Null]
    public string Description;
    public bool IsDeprecated;
    [Null]
    public string DeprecationReason;
  }

  [Hidden]
  public class __Type : IntroObjectBase {
    public __TypeKind Kind;

    // object only 
    [Null]
    public IList<__Type> Interfaces;

    // interface and union only
    [Null]
    public IList<__Type> PossibleTypes;

    // input object only
    [Null]
    public IList<__InputValue> InputFields;

    // non-null and list only 
    [Null]
    public __Type OfType;

    // Fields and EnumValues fields have includeDeprecated parameter, so they are implemented
    //  through GetFields and GetEnumValues resolver methods. 
    [GraphQLName("fields")]
    public IList<__Field> GetFields(bool includeDeprecated = true) { return default; }

    // enum only
    [GraphQLName("enumValues")]
    public IList<__EnumValue> GetEnumValues(bool includeDeprecated = true) { return default; }

    /// <summary>Display name allowing to see the type full name/spec.  The current arrangement in GraphQL
    /// requires unfolding of the entire chain of nested types (NotNull-s and List-s). 
    /// This property is an extension of GraphQL spec. </summary>
    [Null, Hidden, GraphQLName("displayName")]
    public string DisplayName;

    /*
    // The following two lists are for internal use only, they are containers that hold actual lists 
    [Ignore]
    internal IList<__Field> FieldList = new List<__Field>();
    // enum only
    [Ignore]
    internal IList<__EnumValue> EnumValueList = new List<__EnumValue>();
    */

    public __Type() { }
  }

  [Hidden]
  public class __Field: IntroObjectBase {
    public IList<__InputValue> Args;
    public __Type Type; 
  }

  [Hidden]
  public class __InputValue: IntroObjectBase {
    public __Type Type;
    [Null]
    public string DefaultValue; 
  }

  [Hidden]
  public class __EnumValue : IntroObjectBase {
  }


  [Hidden]
  public class __Directive: IntroObjectBase {
    public __DirectiveLocation Locations; 
    public IList<__InputValue> Args;
  }

}
