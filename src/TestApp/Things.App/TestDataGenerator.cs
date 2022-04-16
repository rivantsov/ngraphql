using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Things {

  public static class TestDataGenerator {

    public static void CreateTestData(ThingsApp app) {
      var date0 = DateTime.Now;
      app.Things = new List<Thing>() {
          new Thing() { Name = "Name1", Id = 1, Descr = "Descr1",
            SomeDate = date0, DateQ = date0.AddHours(1), TheKind = ThingKind.KindOne, Tag = "tag1",
            Flags = TheFlags.FlagOne | TheFlags.FlagThree, IntfThing = new ThingEntity(),
            Props = new Dict { {"prop1", "V1" }, { "prop2", 123 } }
          },
          new Thing() { Name = "Name2", Id = 2, Descr = "Descr2",
            SomeDate = date0, DateQ = null, TheKind = ThingKind.KindTwo,
            Flags = TheFlags.FlagTwo, IntfThing = new ThingEntity(),
            Props = new Dict { {"prop1", "V2" }, { "prop3", null } }
          },
          new Thing() { Name = "Name3", Id = 3, Descr = "Descr3",
            SomeDate = date0, DateQ = date0, TheKind = ThingKind.KindThree,
            Flags = TheFlags.FlagOne | TheFlags.FlagTwo, IntfThing = new ThingEntity()
          },
      };
      app.Things[0].NextThing = app.Things[1];
      app.Things[1].NextThing = app.Things[2];

      // Setup child lists/refs OtherThings. MainOtherThing
      int id = 1; 
      foreach(var th in app.Things) {
        th.OtherThings = new OtherThing[] {
          new OtherThing() {Name = $"Other-{th.Id}-a", Id = id++},
          new OtherThing() {Name = $"Other-{th.Id}-b", Id = id++},
          new OtherThing() {Name = $"Other-{th.Id}-c", Id = id++},
        };
        th.MainOtherThing = th.OtherThings[0];
      }
      app.OtherThings = app.Things.SelectMany(th => th.OtherThings).ToList();
    }
  
  } //class
}
