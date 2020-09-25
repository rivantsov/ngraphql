using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  [GraphQLUnion]
  public class SearchResult : Union<Human_, Droid_, Starship_> { }

}
