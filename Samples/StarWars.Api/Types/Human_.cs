using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>A humanoid creature from the Star Wars universe </summary>
  [ObjectType, GraphQLName("Human")]
  public class Human_ : ICharacter_ {

    /// <summary>The ID of the human </summary>
    [Scalar("ID")]
    public string ID { get; set; }

    /// <summary>What this human calls themselves </summary>
    public string Name { get; set; }

    /// <summary>This human&apos;s friends, or an empty list if they have none </summary>
    [Resolver(nameof(StarWarsResolvers.GetFriendsBatched))] // resolver performs batching (impl of DataLoader)
    public IList<ICharacter_> Friends { get; }

    /// <summary>The movies this human appears in </summary>
    public IList<Episode> AppearsIn { get; }

    /// <summary>The home planet of the human, or null if unknown </summary>
    [Null] public string HomePlanet { get; set; }

    /// <summary>Height in the preferred unit, default is meters </summary>
    [GraphQLName("height")]
    public float? GetHeight(LengthUnit unit = LengthUnit.Meter) { return default; }

    /// <summary>Mass in kilograms, or null if unknown </summary>
    public float? Mass { get; set; }

    /// <summary>A list of starships this person has piloted, or an empty list if none </summary>
    public IList<Starship_> Starships { get; }

  }

}
