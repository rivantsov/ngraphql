using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using NGraphQL.CodeFirst;

namespace Things.GraphQL.Types {

  // testing subclassing and abstract classes
  public class ThingBase {
    public int Id { get; set; }
    /// <summary>The name of the thing.</summary>
    public string Name { get; set; }
    // testing bug #13, field in base class is not mapped to resolver, even if there's mapping expr to entity.
    public string StrField;

  }

  /// <summary>A sample GraphQL output object.</summary>
  [Implements(typeof(IIdNameOtherThingsIntf))]
  public class Thing : ThingBase, INamedObj, IObjWithId {
    /// <summary>The description of the thing.</summary>
    [Null]
    public string Description;
    public ThingKind Kind;
    public TheFlags TheFlags;

    public DateTime SomeDateTime;
    public DateTime? DateTimeOpt; //it will be marked as Nullable automatically (no ! mark)
    [Null]
    public Thing NextThing;
    public string IdStr(string prefix) => default;

    [Null, DeprecatedDir("Deprecate-reason1")]
    public string Tag;

    [Resolver(nameof(ThingsResolvers.GetMainOtherThing)), Null]
    public OtherThing MainOtherThing;

    [Resolver(nameof(ThingsResolvers.GetOtherThings))]
    public IList<OtherThing> OtherThings;

    // An example of handling a field with parameters, with intent to use it in strongly-typed client
    // We define 2 members: GetRandoms method that defines the parameters of the field; we set its
    // name is schema using GraphQLName attribute. So it will appear in schema as 'randoms(count: Int!): [Int]'
    // This is the field that 'works' on the server. 
    // We also define Randoms c# field that will be completely ignored by the server, but will be used to receive
    //  the actual value on the client, from JSon matching key-value pair 'randoms' produced by the server-side field
    [GraphQLName("randoms")]
    public int[] GetRandoms(int count = 3) { return null; }

    [Ignore]
    public int[] Randoms;

    public ThingImplInterface IntfThing;

    [Null] public OtherThingWrapper OtherThingWrapped;

    [Null] public Dictionary<string, object> Props;

  }

  // A second GraphQL type mapped to ThingEntity entity. Test of mapping of one entity type to multiple 
  // GraphQL types. We use the same resolver GetThings; but GraphQL field is 'thingsX: [ThingX]!'
  public class ThingX {
    public int IdX;
    public string NameX;
    public ThingKind KindX; 
  }

  public class OtherThing : INamedObj {
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

  // test case for bug #169 in Vita; 
  // this class is GraphQL type without business entity behind; it has ref to OtherThing
  public class OtherThingWrapper {
    public string OtherThingName;
    public DateTime WrappedOn;
    public OtherThing OtherThing; 
  }

  // mapped to interface entity
  public class ThingImplInterface {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
  }

  public class InputObj {
    public int Id;
    public string Name;
    [DeprecatedDir("Num is deprecated. Esc chars: \", \\ ")] // to test escaping in strings
    public int? Num { get; set; }

    [Ignore]
    public object IgnoredProp;

    [Null]
    public string NullableProp;

    public TheFlags Flags;
    public ThingKind Kind;
    [Null] public TheFlags[] FlagsArray;

    [DefaultValue(999), DeprecatedDir("It's not needed anymore.")]
    public int? IntPropWithDefault; 

    // ToString is used in one of the resolvers
    public override string ToString() => $"id:{Id},name:{Name},num:{Num},flags:({Flags}),kind:{Kind}";
  }

  // used in test for fix for bug in issue #27 (all null fields cause error)
  public class InputObjWithNulls {
    public int? Id;
    [Null] public string Name;
    // ToString is used in one of the resolvers
    public override string ToString() => $"id:{Id},name:{Name}";
  }


  public class InputObjWithEnums {
    public TheFlags Flags;
    public ThingKind Kind;
    [Null] public TheFlags[] FlagsArray;

    public override string ToString() {
      var flagsArrStr = FlagsArray == null ? null : string.Join(";", FlagsArray);
      return $"Flags:{Flags};kind:{Kind};FlagsArray:[{flagsArrStr}]";
    }
  }

  public class InputObjParent {
    public int Id;
    public string Name;
    public IList<InputObjChild> ChildObjects;

    public override string ToString() => $"{Id},{Name}, child count: {ChildObjects?.Count}";
  }

  public class InputObjChild {
    public InputObjParent Parent;
    public int Id;
    public string Name;

    public override string ToString() => $"{Id},{Name}";
  }

  public class InputObjWithList {
    public int[][] List;
  }

  public class InputObjWithCustomScalars {
    [Null]
    public Dictionary<string, object> Map;

    public long? MaxLong = long.MaxValue;
    public long? MinLong = long.MinValue; 
    // [Null]  public object AnyValue; //mapped to Any scalar; Any is not enabled yet
  }


  //public class OutObjWithMapAny: InputObjWithCustomScalars { }

  public interface INamedObj {
    string Name { get; }
  }

  public interface IObjWithId {
    int Id { get; }
  }

  // advanced interfaces - intf implement intfaces, covariance of impl
  public interface IIdNameIntf {
    int Id { get; }
    string Name { get; }
  }

  // An example of covariant implementation of interface field. Covariance and type compatibility: IList<NamedObj> <=> IList<ThingEntity>
  [Implements(typeof(IIdNameIntf))]
  public interface IIdNameOtherThingsIntf {
    int Id { get; }
    string Name { get; }
    IList<INamedObj> OtherThings { get; }
  }

  // unions 
  public class ThingsUnion : Union<Thing, OtherThing> {
    public ThingsUnion(ThingEntity value) {
      Value = value;
    }
    public ThingsUnion(OtherThingEntity value) {
      Value = value;
    }
  }

}
