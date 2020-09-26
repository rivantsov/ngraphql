using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>The input object sent when passing in a color</summary>
  [GraphQLInput("ColorInput")]
  public class ColorInput_ {

    public int Red;

    public int Green;

    public int Blue;
  }
}
