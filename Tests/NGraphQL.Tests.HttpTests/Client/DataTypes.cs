using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NGraphQL.Client;

namespace NGraphQL.Tests.HttpTests.Client {

  // client-side data types, they mirror server-side classes, with a few changes
  //  for ex, methods (fields with args) are replaced with fields of type matching return type of the method.

  public enum ThingKind {
    KindOne,
    KindTwo,
    KindThree,
  }

  [Flags]
  public enum TheFlags {
    None = 0, 
    FlagOne = 1,
    FlagTwo = 1 << 1,
    FlagThree = 1 << 2,
  }

  public class Thing: ClientDataType {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description;
    public ThingKind Kind;
    public TheFlags TheFlags;

    public DateTime SomeDateTime;
    public DateTime? DateTimeOpt; //it will be marked as Nullable automatically (no ! mark)
    public Thing NextThing;
    public string Tag;

    public OtherThing MainOtherThing;
    public IList<OtherThing> otherThings;

    // getRandoms is a method with single parameter. We create a field here to hold the return value
    [JsonProperty("getRandoms")]
    public int[] Randoms;
  }

  public class OtherThing: ClientDataType {
    public string IdStr;
    public string Name { get; set; }
    public DateTime DateValue;
    public TimeSpan TimeValue; //timespan maps to Time automatically
    public string[] Strings;
    public string[] StringsWithNulls;
    public int?[] IntsWithNulls; 
    public string NameOrThrow;
    public string GetNameOrThrow;
    public string GetNameOrThrowAsync;
  }

}
