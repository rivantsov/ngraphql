using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  public class StarWarsApiModule: GraphQLModule {
    public StarWarsApiModule(GraphQLApi api): base(api) {
      // Register types
      RegisterTypes(
        typeof(IStarWarsQuery), typeof(IStarWarsMutation),
        typeof(Episode), typeof(LengthUnit),
        typeof(ICharacter_), typeof(Human_), typeof(Droid_), 
        typeof(Starship_), typeof(Review_), typeof(SearchResult_), typeof(ReviewInput_)
        );

      // map app entity types to GraphQL Api types
      MapEntity<Human>().To<Human_>(h => new Human_() {
        Mass = h.MassKg, // we use custom mapping expression here, others with matching names are mapped automatically
      });
      MapEntity<Droid>().To<Droid_>();
      MapEntity<Starship>().To<Starship_>();
      MapEntity<Review>().To<Review_>();
      MapEntity<Character>().To<ICharacter_>();
      MapEntity<NamedObject>().ToUnion<SearchResult_>();
      
      //resolvers
      RegisterResolvers(typeof(StarWarsResolvers));

    } //constructor  

  }
}
