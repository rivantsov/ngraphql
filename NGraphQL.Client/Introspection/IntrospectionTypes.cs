using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Client.Introspection {

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

  public enum __DirectiveLocation {
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

  public class __Schema {
    public IList<__Type> Types = new List<__Type>();
    public __Type QueryType;
    public __Type MutationType;
    public __Type SubscriptionType;
    public IList<__Directive> Directives = new List<__Directive>();
  }

  public abstract class IntroObjectBase: ClientDataType {
    public string Name;
    public string Description;
    public bool IsDeprecated;
    public string DeprecationReason;
  }

  public class __Type : IntroObjectBase {
    public __TypeKind Kind;

    // object only 
    public IList<__Type> Interfaces;

    // interface and union only
    public IList<__Type> PossibleTypes;

    // input object only
    public IList<__InputValue> InputFields;

    // non-null and list only 
    public __Type OfType;

    public IList<__Field> Fields;

    // enum only
    public IList<__EnumValue> EnumValues;

    /// <summary>Display name allowing to see the type name when it is List or NotNull - the current arrangement in GraphQL
    /// is quite stupid. This is extension of GraphQL spec. </summary>
    public string DisplayName;
  }

  public class __Field: IntroObjectBase {
    public IList<__InputValue> Args;
    public __Type Type; 
  }

  public class __InputValue: IntroObjectBase {
    public __Type Type;
    public string DefaultValue; 
  }

  public class __EnumValue : IntroObjectBase {
  }


  public class __Directive: IntroObjectBase {
    public __DirectiveLocation Locations; 
    public IList<__InputValue> Args;
  }

}
