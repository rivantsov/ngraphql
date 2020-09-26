using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  [Query]
  public class StarWarsQuery {

    [GraphQLName("characters")]
    public IList<ICharacter_> GetCharacters(Episode episode) { return default; }

    [GraphQLName("starships")]
    public IList<Starship_> GetStarships() { return default; }

    [GraphQLName("reviews")]
    public IList<Review_> GetReviews(Episode episode) { return default; }

    [GraphQLName("character")]
    public ICharacter_ GetCharacter([Scalar("ID")] string id) { return default; }

    [GraphQLName("starship")]
    public Starship_ GetStarship([Scalar("ID")] string id) { return default; }

    public IList<SearchResult_> Search(string text) { return default; }
  }
}
