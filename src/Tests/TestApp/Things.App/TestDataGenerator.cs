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
      app.Things = new List<ThingEntity>() {
          new ThingEntity() { Name = "Name1", Id = 1, Descr = "Descr1",
            SomeDate = date0, DateQ = date0.AddHours(1), TheKind = ThingKind.KindOne, Tag = "tag1",
            Flags = TheFlags.FlagOne | TheFlags.FlagThree, IntfThing = new ThingWithInterfaceEntity(),
            Props = new Dict { {"prop1", "V1" }, { "prop2", 123 } }
          },
          new ThingEntity() { Name = "Name2", Id = 2, Descr = "Descr2",
            SomeDate = date0, DateQ = null, TheKind = ThingKind.KindTwo, Tag = "tag2",
            Flags = TheFlags.FlagTwo, IntfThing = new ThingWithInterfaceEntity(),
            Props = new Dict { {"prop1", "V2" }, { "prop3", null } }
          },
          new ThingEntity() { Name = "Name3", Id = 3, Descr = "Descr3",
            SomeDate = date0, DateQ = date0, TheKind = ThingKind.KindThree,  Tag = "tag3",
            Flags = TheFlags.FlagOne | TheFlags.FlagTwo, IntfThing = new ThingWithInterfaceEntity()
          },
          // used in subscriptions
          new ThingEntity() { Name = "Name4", Id = 4, Descr = "Descr4",
            SomeDate = date0, DateQ = date0, TheKind = ThingKind.KindOne,  Tag = "tag4",
            Flags = TheFlags.FlagOne, IntfThing = new ThingWithInterfaceEntity()
          },
          new ThingEntity() { Name = "Name5", Id = 5, Descr = "Descr5",
            SomeDate = date0, DateQ = null, TheKind = ThingKind.KindTwo,  Tag = "tag5",
            Flags = TheFlags.FlagOne | TheFlags.FlagTwo, IntfThing = new ThingWithInterfaceEntity()
          },
          new ThingEntity() { Name = "Name6", Id = 6, Descr = "Descr6",
            SomeDate = date0, DateQ = date0, TheKind = ThingKind.KindThree,  Tag = "tag6",
            Flags = TheFlags.FlagOne | TheFlags.FlagThree, IntfThing = new ThingWithInterfaceEntity()
          },


      };
      app.Things[0].NextThing = app.Things[1];
      app.Things[1].NextThing = app.Things[2];

      // Setup child lists/refs OtherThings. MainOtherThing
      int id = 1; 
      foreach(var th in app.Things) {
        th.OtherThings = new OtherThingEntity[] {
          new OtherThingEntity() {Name = $"Other-{th.Id}-a", Id = id++},
          new OtherThingEntity() {Name = $"Other-{th.Id}-b", Id = id++},
          new OtherThingEntity() {Name = $"Other-{th.Id}-c", Id = id++},
        };
        th.MainOtherThing = th.OtherThings[0];
      }
      app.OtherThings = app.Things.SelectMany(th => th.OtherThings).ToList();
    }
  
  } //class
}
