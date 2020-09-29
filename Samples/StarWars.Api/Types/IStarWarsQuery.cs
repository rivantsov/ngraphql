using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  [Query]
   interface IStarWarsQuery {

    [GraphQLName("characters")]
     IList<ICharacter_> GetCharacters(Episode episode);

    [GraphQLName("starships")]
     IList<Starship_> GetStarships();

    [GraphQLName("reviews")]
     IList<Review_> GetReviews(Episode episode);

    [GraphQLName("character")]
     ICharacter_ GetCharacter([Scalar("ID")] string id);

    [GraphQLName("starship")]
     Starship_ GetStarship([Scalar("ID")] string id);

     IList<SearchResult_> Search(string text);
  }
}
