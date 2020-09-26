using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;

namespace NGraphQL.Model.Introspection {

  // Identifiers like __Schema are not CLS-compliant, so we use suffix form instead (Schema__) for class names
  //  and specify GraphQL name explicitly using attribute

  [Hidden]
  [GraphQLObjectType, GraphQLName("__Schema")]
  public class Schema__ {
    public IList<Type__> Types = new List<Type__>();
    public Type__ QueryType;
    public Type__ MutationType;
    public Type__ SubscriptionType;

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
  [GraphQLObjectType, GraphQLName("__Type")]
  public class Type__ : IntroObjectBase {
    public TypeKind Kind;

    // object only 
    [Null]
    public IList<Type__> Interfaces;

    // interface and union only
    [Null]
    public IList<Type__> PossibleTypes;

    // input object only
    [Null]
    public IList<InputValue__> InputFields;

    // non-null and list only 
    [Null]
    public Type__ OfType;

    // Fields and EnumValues fields have includeDeprecated parameter, so they are implemented
    //  through GetFields and GetEnumValues resolver methods. 
    [GraphQLName("fields")]
    public IList<Field__> GetFields(bool includeDeprecated = true) { return default; }

    // enum only
    [GraphQLName("enumValues")]
    public IList<EnumValue__> GetEnumValues() { return default; }

    /// <summary>Display name allowing to see the type name when it is List or NotNull - the current arrangement in GraphQL
    /// is quite stupid. This is extension of GraphQL spec. </summary>
    [Null, Hidden, GraphQLName("displayName")]
    public string DisplayName;

    // The following two lists are for internal use only, they are containers that hold actual lists 
    [GraphQLIgnore]
    internal IList<Field__> FieldList = new List<Field__>();
    // enum only
    [GraphQLIgnore]
    internal IList<EnumValue__> EnumValueList = new List<EnumValue__>();

    [GraphQLIgnore]
    internal TypeDefBase TypeDef; // internally used link to model object

    public Type__() { }
  }

  [Hidden]
  [GraphQLObjectType, GraphQLName("__Field")]
  public class Field__: IntroObjectBase {
    public IList<InputValue__> Args;
    public Type__ Type; 
  }

  [Hidden]
  [GraphQLObjectType, GraphQLName("__InputValue")]
  public class InputValue__: IntroObjectBase {
    public Type__ Type;
    [Null]
    public string DefaultValue; 
  }

  [Hidden]
  [GraphQLObjectType, GraphQLName("__EnumValue")]
  public class EnumValue__ : IntroObjectBase {
  }


  [Hidden]
  [GraphQLObjectType, GraphQLName("__Directive")]
  public class Directive__ : IntroObjectBase {
    public DirectiveLocation Locations; 
    public IList<InputValue__> Args;
  }

}
