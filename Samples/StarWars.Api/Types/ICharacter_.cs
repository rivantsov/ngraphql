using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>A character from the Star Wars universe </summary>
  public interface ICharacter_ {

    /// <summary>The ID of the character </summary>
    [Scalar("ID")]
    string ID { get; set; }

    /// <summary>The name of the character </summary>
    string Name { get; set; }

    /// <summary>The friends of the character, or an empty list if they have none </summary>
    IList<ICharacter_> Friends { get; }

    /// <summary>The movies this character appears in </summary>
    IList<Episode> AppearsIn { get; }
  }

}
