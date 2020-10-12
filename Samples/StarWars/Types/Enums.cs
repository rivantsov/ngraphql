using System;

namespace StarWars {

  /// <summary>The episodes in the Star Wars trilogy</summary>
  public enum Episode {
    /// <summary>Star Wars Episode IV: A New Hope, released in 1977. </summary>
    Newhope,
    
    /// <summary>Star Wars Episode V: The Empire Strikes Back, released in 1980. </summary>
    Empire,
    
    /// <summary>Star Wars Episode VI: Return of the Jedi, released in 1983. </summary>
    Jedi
  }

  /// <summary>Units of length or height.</summary>
  public enum LengthUnit {
    /// <summary>The standard unit around the world.</summary>
    Meter,

    /// <summary>Primarily used in the United States.</summary>
    Foot,
  }

  /// <summary>
  /// Encodes emoji(s) accompanying review. This enum is added to demonstrate use of multi-valued (Flags)
  /// enums in NGraphQL
  /// </summary>
  [Flags]
  public enum Emojis {
    None = 0, 

    Like = 1, 

    Dislike = 1 << 1,

    Smile = 1 << 2, 

    Angry = 1 << 3,

    Fear = 1 << 4,
  }
}
