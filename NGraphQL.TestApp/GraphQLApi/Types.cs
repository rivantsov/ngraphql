using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  /// <summary>A sample GraphQL output object.</summary>
  [GraphQLObjectType] 
  public class Thing_ : INamedObj, IObjWithId {
    public int Id { get; set; }
    public string Name { get; set; }
    [Null]
    public string Description;
    public ThingKind Kind;
    public TheFlags TheFlags;

    public DateTime SomeDateTime;
    public DateTime? DateTimeOpt; //it will be marked as Nullable automatically (no ! mark)
    [Null]
    public Thing_ NextThing;
    [Null]
    public string Tag;

    [Resolver(nameof(ThingsApiResolvers.GetMainOtherThing)), Null]
    public OtherThing_ MainOtherThing;

    [Resolver(nameof(ThingsApiResolvers.GetOtherThings))]
    public IList<OtherThing_> OtherThings;
  }

  [GraphQLObjectType]
  public class OtherThing_ : INamedObj {
    [Scalar("ID")]
    public string IdStr;
    public string Name { get; set; }

    /// <summary>This is a date field - this description comes from Xml comment in type definition.</summary>
    [Scalar("Date")]
    public DateTime DateValue;
    public TimeSpan TimeValue; //timespan maps to Time automatically

    public string[] Strings;
    [WithNulls, Null]
    public string[] StringsWithNulls;

    public int?[] IntsWithNulls; // -> [Int]!

    public string NameOrThrow;

    public string GetNameOrThrow() { return default; }
    public string GetNameOrThrowAsync() { return default; }
  }


  [GraphQLInput]
  public class InputObj {
    public int Id;
    public string Name;
    [DeprecatedDir("Num is deprecated. Esc chars: \", \\ ")] // to test escaping in strings
    public int? Num { get; set; }
    // ToString is used in one of the resolvers
    public override string ToString() => $"id:{Id},name:{Name},num:{Num}";
  }

  [GraphQLInput]
  public class InputObjWithEnums {
    public TheFlags Flags;
    public ThingKind Kind;
    [Null] public TheFlags[] FlagsArray;

    public override string ToString() {
      var flagsArrStr = FlagsArray == null ? null : string.Join(";", FlagsArray);
      return $"Flags:{Flags};kind:{Kind};FlagsArray:[{flagsArrStr}]";
    }
  }

  [GraphQLInput]
  public class InputObjParent {
    public int Id;
    public string Name;
    public IList<InputObjChild> ChildObjects;

    public override string ToString() => $"{Id},{Name}, child count: {ChildObjects?.Count}";
  }

  [GraphQLInput]
  public class InputObjChild {
    public InputObjParent Parent;
    public int Id;
    public string Name;

    public override string ToString() => $"{Id},{Name}";
  }

  [GraphQLInput]
  public class InputObjWithList {
    public int[][] List;
  }


  [GraphQLInterface]
  public interface INamedObj {
    string Name { get; }
  }

  [GraphQLInterface]
  public interface IObjWithId {
    int Id { get; }
  }


  [GraphQLUnion]
  public class ThingsUnion : Union<Thing, OtherThing> {
    public ThingsUnion(Thing value) {
      Value = value;
    }
    public ThingsUnion(OtherThing value) {
      Value = value;
    }
  }

}
