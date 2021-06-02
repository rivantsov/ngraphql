using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Things {

  /// <summary>Thing kind enumeration.</summary>  
  public enum ThingKind {
    /// <summary>Thing kind #1.</summary>  
    KindOne,
    KindTwo,

    /// <summary>This is kind Three </summary>
    KindThree,

    // this value will be ignored by model builder - it will not show up in schema doc. 
    // an example how to hide a enum value using code, when you do not control enum definition
    KindFour_Ignored,
  }

  /// <summary>Thing flags enumeration.</summary>  
  [Flags]
  public enum TheFlags {
    None = 0, // this will not appear in GraphQL enum: 0-valued values in Flags enums are ignored automatically
    FlagOne = 1,
    /// <summary>Description for FlagTwo loaded from Xml comment.</summary>
    FlagTwo = 1 << 1,
    FlagThree = 1 << 2,
  }

  // testing entity subclassing
  public class ThingBase {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  [DebuggerDisplay("Thing: {Name}/{TheKind}")]
  public class Thing: ThingBase {
    public string Descr;
    public ThingKind TheKind;
    public TheFlags Flags;
    public DateTime SomeDate;
    public DateTime? DateQ;
    public Thing NextThing;

    // For testing batching
    public OtherThing MainOtherThing;
    public IList<OtherThing> OtherThings; 

    public string Tag;

    public IThingIntfEntity IntfThing; 
  }

  [DebuggerDisplay("{Name}")]
  public class OtherThing {
    public int Id;
    public string Name { get; set; }

    public string NameOrThrow {
      get {
        if(this.Id == 5)
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
  public interface IThingIntfEntityBase {
    int Id { get; }
    string Name { get; }
  }

  public interface IThingIntfEntity : IThingIntfEntityBase {
    string Tag { get; }
  }

  public class ThingEntity : IThingIntfEntity {
    public int Id { get; set; } = _id++;
    public string Name { get; set; } = "name" + _id;
    public string Tag { get; set; } = "tag" + _id; 

    private static int _id; 
  }
  #endregion



}
