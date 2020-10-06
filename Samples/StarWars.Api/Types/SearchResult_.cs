using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  public class SearchResult_ : Union<Human_, Droid_, Starship_> { }

}
