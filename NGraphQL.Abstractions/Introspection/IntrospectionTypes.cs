using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

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
    public TypeKind Kind;

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
    public DirectiveLocation Locations; 
    public IList<__InputValue> Args;
  }

}
