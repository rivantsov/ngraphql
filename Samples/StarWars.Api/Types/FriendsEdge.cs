using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>An edge object for a character's friends </summary>
  [GraphQLObjectType("FriendsEdge")]
  public class FriendsEdge_ {

    /// <summary>A cursor used for pagination </summary>
    [Scalar("ID")]
    public string Cursor;

    /// <summary>The character represented by this friendship edge. </summary>
    [Null]
    public ICharacter_ Node;
  }

}
