using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Things {

  /// <summary>ThingEntity kind enumeration.</summary>  
  public enum ThingKind {
    /// <summary>ThingEntity kind #1.</summary>  
    KindOne,
    KindTwo,

    /// <summary>This is kind Three </summary>
    KindThree,

    // this value will be ignored by model builder - it will not show up in schema doc. 
    // an example how to hide a enum value using code, when you do not control enum definition
    KindFour_Ignored,
  }

  /// <summary>ThingEntity flags enumeration.</summary>  
  [Flags]
  public enum TheFlags {
    None = 0, // this will not appear in GraphQL enum: 0-valued values in Flags enums are ignored automatically
    FlagOne = 1,
    /// <summary>Description for FlagTwo loaded from Xml comment.</summary>
    FlagTwo = 1 << 1,
    FlagThree = 1 << 2,
  }

  // testing entity subclassing
  public class ThingBaseEntity {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  [DebuggerDisplay("Thing: {Name}/{TheKind}")]
  public class ThingEntity: ThingBaseEntity {
    public string Descr;
    public ThingKind TheKind;
    public TheFlags Flags;
    public DateTime SomeDate;
    public DateTime? DateQ;
    public ThingEntity NextThing;


    // For testing batching
    public OtherThingEntity MainOtherThing;
    public IList<OtherThingEntity> OtherThings; 

    public string Tag;

    public IExtCustomInterface IntfThing;

    public Dictionary<string, object> Props;
  }

  [DebuggerDisplay("{Name}")]
  public class OtherThingEntity {
    public int Id;
    public string Name { get; set; }

    public string NameOrThrow {
      get {
        if (this.Id == 5)
          throw new Exception("Exception thrown by NameOrThrow.");
        return Name; 
      }
    }

    public string IdStr => Id.ToString();
    public DateTime DateValue;
    public TimeSpan TimeValue; //timespan maps to Time automatically
    public string[] Strings;
    public string[] StringsWithNulls;
    public int?[] IntsWithNulls; // -> [Int]!

  }

  #region entities as interfaces
  // testing entities as interfaces, with inheritance - the case for VITA ORM
  public interface ICustomInterface {
    int Id { get; }
    string Name { get; }
  }

  public interface IExtCustomInterface : ICustomInterface {
    string Tag { get; }
  }

  public class ThingWithInterfaceEntity : IExtCustomInterface {
    public int Id { get; set; } = _id++;
    public string Name { get; set; } = "name" + _id;
    public string Tag { get; set; } = "tag" + _id; 

    private static int _id; 
  }
  #endregion



}
