using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>Represents a review for a movie </summary>
  [ObjectType]
  public class Review_ {

    /// <summary>The movie </summary>
    public Episode Episode;

    /// <summary>The number of stars this review gave, 1-5 </summary>
    public int Stars;

    /// <summary>Comment about the movie </summary>
    public string Commentary;
  }

}
