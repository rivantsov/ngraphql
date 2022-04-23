using System;
using System.Collections.Generic;
using System.Linq;
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
    // we make it virtual to override in __Type and add [Null] attr - Type__ might have Name=null (ex: non-null wrapper)
    public virtual string Name { get; set; }
    [Null]
    public string Description;
    public bool IsDeprecated;
    [Null]
    public string DeprecationReason;
  }

  [Hidden]
  public class __Type : IntroObjectBase {
    [Null]
    public override string Name {get;set;}
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

    // Scalar only 
    [Null]
    public string SpecifiedBy; 

    [GraphQLName("fields")]
    public IList<__Field> GetFields(bool includeDeprecated = true) {
      throw new NotImplementedException("GetFields is a server-side method and should not be called directly.");
    }

    // enum only
    [GraphQLName("enumValues")]
    public IList<__EnumValue> GetEnumValues(bool includeDeprecated = true) {
      throw new NotImplementedException("GetEnumValues is a server-side method and should not be called directly.");
    }

    // The displayName property is an extension of GraphQL spec. 
    // The current arrangement in GraphQL requires unfolding of the entire chain of nested types (NotNull-s and List-s). 
    /// <summary>Full type spec, ex: [Int!]! .</summary>
    [Null]
    public string DisplayName;

    // these 2 properties will be ignored on the server. When used on the client, they will be receiving 
    //  data from Fiels and Enum values received from the server-side GetFields and GetEnumValues fields/methods 
    [Ignore]
    public IList<__Field> Fields = new List<__Field>();
    // enum only
    [Ignore]
    public IList<__EnumValue> EnumValues = new List<__EnumValue>();

    public __Type() { }

    public override string ToString() => DisplayName;
  }

  [Hidden]
  public class __Field: IntroObjectBase {
    public IList<__InputValue> Args = new List<__InputValue>();
    public __Type Type;
    [Hidden]
    public bool IsHidden; 
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
    public bool IsRepeatable;
    public IList<__InputValue> Args = new List<__InputValue>();
  }

}
