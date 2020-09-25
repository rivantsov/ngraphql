using System;

namespace StarWars {

  /// <summary>The episodes in the Star Wars trilogy</summary>
  [Flags]
  public enum Episode {
    /// <summary>Special zero value for internal use, matches empty enum list in GraphQL will not appear in GraphQL schema. </summary>
    None = 0,

    /// <summary>Star Wars Episode IV: A New Hope, released in 1977. </summary>
    Newhope = 1,
    
    /// <summary>Star Wars Episode V: The Empire Strikes Back, released in 1980. </summary>
    Empire = 1 << 1,
    
    /// <summary>Star Wars Episode VI: Return of the Jedi, released in 1983. </summary>
    Jedi = 1 << 2
  }

  /// <summary>Units of length or height.</summary>
  public enum LengthUnit {
    /// <summary>The standard unit around the world.</summary>
    Meter,

    /// <summary>Primarily used in the United States.</summary>
    Foot,
  }


}
