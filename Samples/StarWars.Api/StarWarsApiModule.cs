using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  public class StarWarsApiModule: GraphQLModule {
    public StarWarsApiModule() {
      RegisterTypes(typeof(Episode), typeof(LengthUnit),
        typeof(ICharacter_), typeof(Human_), typeof(Droid_), 
        typeof(Starship_), typeof(Review_),
        typeof(FriendsConnection_), typeof(FriendsEdge_),
        typeof(ReviewInput_),  typeof(ColorInput_),
        typeof(SearchResult), typeof(PageInfo_)
        );

      RegisterResolversClass<ApiResolvers>(); 
    } //constructor
  
  }
}
