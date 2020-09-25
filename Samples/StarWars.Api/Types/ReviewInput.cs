using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>The input object sent when someone is creating a new review </summary>
  [GraphQLInput("ReviewInput")]
  public class ReviewInput_ {

    /// <summary>0-5 stars </summary>
    public int Stars;

    /// <summary>Comment about the movie, optional </summary>
    public string Commentary;

    /// <summary>Favorite color, optional </summary>
    [GraphQLName("favorite_color"), Null]
    public ColorInput_ FavoriteColor;
  }

}
