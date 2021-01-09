using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Core;

namespace NGraphQL.TestApp {

  /****************************************************************************************
   A sample 'business' app behind the sample GraphQLApi, managing 'Things'
  ****************************************************************************************/

  /// <summary>Thing kind enumeration.</summary>  
  public enum ThingKind {
    /// <summary>Thing kind #1.</summary>  
    KindOne,
    KindTwo,
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
    FlagTwo = 1 << 1,
    FlagThree = 1 << 2,
  }

  [DebuggerDisplay("Thing: {Name}/{TheKind}")]
  public class Thing {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Descr;
    public ThingKind TheKind;
    public TheFlags Flags;
    public DateTime SomeDate;
    public DateTime? DateQ;
    public Thing NextThing;

    // For testing batching
    public OtherThing MainOtherThing;
    public IList<OtherThing> OtherThings; 

    [DeprecatedDir("Deprecate-reason1")]
    public string Tag; 
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

  public class ThingsApp {
    public static ThingsApp Instance;

    // it is just things and other-things
    public List<Thing> Things;
    public List<OtherThing> OtherThings; 

    public ThingsApp() {
      Instance = this; 
      CreateTestData(); 
    }

    // The method used in async call test. 
    // WaitValue is initially -1; the caller test method calls this method, awaits return; 
    // the returned task should be in 'Running' status.
    // The code then changes the WaitValue to positive value and waits for task to complete. 
    // This is a test that stack completely unwinds in long-running async method
    public static int WaitValue;
    public async Task<int> WaitForPositiveValueAsync(CancellationToken cancellationToken) {
      while(WaitValue < 0 && !cancellationToken.IsCancellationRequested)
        await Task.Delay(100);
      return WaitValue;
    }

    private void CreateTestData() {
      var date0 = DateTime.Now;
      Things = new List<Thing>() {
          new Thing() { Name = "Name1", Id = 1, Descr = "Descr1",
            SomeDate = date0, DateQ = date0.AddHours(1), TheKind = ThingKind.KindOne,
            Flags = TheFlags.FlagOne | TheFlags.FlagThree},
          new Thing() { Name = "Name2", Id = 2, Descr = "Descr2",
            SomeDate = date0, DateQ = null, TheKind = ThingKind.KindTwo,
            Flags = TheFlags.FlagTwo},
          new Thing() { Name = "Name3", Id = 3, Descr = "Descr3",
            SomeDate = date0, DateQ = date0, TheKind = ThingKind.KindThree,
            Flags = TheFlags.FlagOne | TheFlags.FlagTwo},
      };
      Things[0].NextThing = Things[1];
      Things[1].NextThing = Things[2];

      // Setup child lists/refs OtherThings. MainOtherThing
      int id = 1; 
      foreach(var th in Things) {
        th.OtherThings = new OtherThing[] {
          new OtherThing() {Name = $"Other-{th.Id}-a", Id = id++},
          new OtherThing() {Name = $"Other-{th.Id}-b", Id = id++},
          new OtherThing() {Name = $"Other-{th.Id}-c", Id = id++},
        };
        th.MainOtherThing = th.OtherThings[0];
      }

      OtherThings = Things.SelectMany(th => th.OtherThings).ToList();
    }
  
  } //class
}
