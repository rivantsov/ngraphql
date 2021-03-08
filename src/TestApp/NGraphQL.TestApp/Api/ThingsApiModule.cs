using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  // Api definitions (types, resolvers) are first orginized into api modules,
  // modules then  assembled into a GraphQLApi
  
  public class ThingsGraphQLModule : GraphQLModule {
    public ThingsGraphQLModule() {
      // 1. Register all types
      base.EnumTypes.Add(typeof(ThingKind), typeof(TheFlags));
      base.ObjectTypes.Add(typeof(Thing_), typeof(OtherThing_), typeof(ThingForIntfEntity_), typeof(OtherThingWrapper_));
      base.InputTypes.Add(typeof(InputObj), typeof(InputObjWithEnums), typeof(InputObjParent),         
        typeof(InputObjChild),  typeof(InputObjWithList));
      base.InterfaceTypes.Add(typeof(INamedObj), typeof(IObjWithId));
      base.UnionTypes.Add(typeof(ThingsUnion));

      base.QueryType = typeof(IThingsQuery);
      base.MutationType = typeof(IThingsMutation);
      base.SubscriptionType = typeof(IThingsSubscription);

      // Define mappings of entities (biz app objects) to API Object Types 
      MapEntity<Thing>().To<Thing_>(th => new Thing_() {
        Id = th.Id,
        Name = th.Name,
        Description = th.Descr,
        Kind = th.TheKind,
        TheFlags = th.Flags,
        DateTimeOpt = th.DateQ,
        SomeDateTime = th.SomeDate,
        // example of using FromMap function to explicitly convert biz object to API object (BizThing => ApiThing)
        // Note: we could skip this, as field names match, it would automap
        NextThing = FromMap<Thing_>(th.NextThing), 
        OtherThingWrapped = th.MainOtherThing.GetWrapper(),        
      });

      MapEntity<OtherThing>().To<OtherThing_>(); // engine will automatically map all matching fields
      MapEntity<IThingIntfEntity>().To<ThingForIntfEntity_>();

      this.ResolverTypes.Add(typeof(ThingsResolvers));

      // testing hide-enum-value feature. Use this if you have no control over enum declaration, but you want to 
      //  remove/hide some members; for ex, some flag enums declare extra flag combinations as enum members (I do this often),
      //  this practice does not fit with GraphQL semantics, so these values should be removed from the GraphQL enum declaration/schema. 
      this.IgnoreMember(typeof(ThingKind), nameof(ThingKind.KindFour_Ignored));
    }// constructor

  } // class


}
