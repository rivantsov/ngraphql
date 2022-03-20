using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

using Things.GraphQL.Types;

namespace Things.GraphQL {

  // Api definitions (types, resolvers) are first orginized into api modules,
  // modules then  assembled into a GraphQLApi

  public class ThingsGraphQLModule : GraphQLModule {
    public ThingsGraphQLModule() {
      // 1. Register all types
      base.EnumTypes.Add(typeof(ThingKind), typeof(TheFlags));
      base.ObjectTypes.Add(typeof(Thing_), typeof(OtherThing_),
             typeof(ThingForIntfEntity_), typeof(OtherThingWrapper_), typeof(ThingX_));
      base.InputTypes.Add(typeof(InputObj), typeof(InputObjWithEnums), typeof(InputObjParent),         
        typeof(InputObjChild),  typeof(InputObjWithList), typeof(InputObjWithMap));
      base.InterfaceTypes.Add(typeof(INamedObj), typeof(IObjWithId));
      base.UnionTypes.Add(typeof(ThingsUnion));

      base.QueryType = typeof(IThingsQuery);
      base.MutationType = typeof(IThingsMutation);
      base.SubscriptionType = typeof(IThingsSubscription);

      // Define mappings of entities (biz app objects) to API Object Types 
      MapEntity<Thing>().To<Thing_>(th => new Thing_() {
        Id = th.Id,
        Name = th.Name,
        StrField = th.Name + "-EXT",
        Description = th.Descr,
        Kind = th.TheKind,
        TheFlags = th.Flags,
        DateTimeOpt = th.DateQ,
        SomeDateTime = th.SomeDate,
        // example of using FromMap function to explicitly convert biz object to API object (BizThing => ApiThing)
        // Note: we could skip this, as field names match, it would automap
        NextThing = FromMap<Thing_>(th.NextThing), 
        OtherThingWrapped = CreateOtherThingWrapper(th.MainOtherThing),        
      });

      // map Thing to another GrqphQL type ThingX_
      MapEntity<Thing>().To<ThingX_>(th => new ThingX_() {
        IdX = th.Id,
        NameX = th.Name,
        KindX = th.TheKind,
      });


      MapEntity<OtherThing>().To<OtherThing_>(); // engine will automatically map all matching fields
      MapEntity<IThingIntfEntity>().To<ThingForIntfEntity_>();

      // testing hide-enum-value feature. Use this if you have no control over enum declaration, but you want to 
      //  remove/hide some members; for ex, some flag enums declare extra flag combinations as enum members (I do this often),
      //  this practice does not fit with GraphQL semantics, so these values should be removed from the GraphQL enum declaration/schema. 
      this.IgnoreMember(typeof(ThingKind), nameof(ThingKind.KindFour_Ignored));

      // Resolvers
      this.ResolverClasses.Add(typeof(ThingsResolvers));

    }// constructor

    // testing bug fix
    private static OtherThingWrapper_ CreateOtherThingWrapper(OtherThing otherTh) {
      if (otherTh == null)
        return null;
      return new OtherThingWrapper_() {
        OtherThingName = otherTh.Name, WrappedOn = DateTime.Now,
        OtherThing = new OtherThing_() { IdStr = otherTh.IdStr, Name = otherTh.Name }
      };
    }

  } // class


}
