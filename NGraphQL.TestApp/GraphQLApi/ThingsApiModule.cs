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
      RegisterTypes(
        typeof(Query_), typeof(ThingsMutation), typeof(IThingsSubscription),
        typeof(ThingKind), typeof(TheFlags),
        typeof(INamedObj), typeof(IObjWithId),
        typeof(Thing_), typeof(OtherThing_),
        typeof(InputObj), typeof(InputObjWithEnums), typeof(InputObjParent), typeof(InputObjChild),
        typeof(InputObjWithList),
        typeof(ThingsUnion)
        );

      // Define mappings of entities (biz app objects) to API Object Types 
      Map<Thing, Thing_>(bt => new Thing_() {
        Id = bt.Id,
        Name = bt.Name,
        Description = bt.Descr,
        Kind = bt.TheKind,
        TheFlags = bt.Flags,
        DateTimeOpt = bt.DateQ,
        SomeDateTime = bt.SomeDate,
        // example of using FromMap function to explicitly convert biz object to API object (BizThing => ApiThing)
        // Note: we could skip this, as field names match, it would automap
        NextThing = FromMap<Thing_>(bt.NextThing)
        
      });
      Map<OtherThing, OtherThing_>(); // engine will automatically map Name->Name
      
      // register resolver classes
      base.RegisterResolversClass<ThingsApiResolvers>();
    }// constructor

    public override void OnModelConstructed(GraphQLApi api) {
      base.OnModelConstructed(api);
      // testing hide-enum-value feature. Use this if you have no control over enum declaration, but you want to 
      //  remove/hide some members; for ex, some flag enums declare extra flag combinations as enum members (I do this often),
      //  this practice does not fit with GraphQL semantics, so these values should be removed from the GraphQL enum declaration/schema. 
      api.Model.RemoveEnumValue(ThingKind.KindFour_Hidden);
    }
  } // class

}
