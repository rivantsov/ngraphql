using System;
using System.Collections.Generic;
using System.Text;

namespace StarWars {

  public class Human : Character {
    public string HomePlanet;
    public float? Mass;
    public float? Height;
    public IList<Starship> Starships;
    public IList<string> StarshipIds;

  }

}
