using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  [Mutation]
  public class StarWarsMutation {

    public Review_ CreateReview(Episode episode, ReviewInput_ reviewInput) {
      return default;
    }
  }
}
