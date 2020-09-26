using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>A connection object for a character's friends </summary>
  [GraphQLObjectType("FriendsConnection")]
  public class FriendsConnection_ {

    /// <summary>The total number of friends </summary>
    public int TotalCount;

    /// <summary>The edges for each of the character's friends. </summary>
    public IList<FriendsEdge_> Friends;

    /// <summary>Information for paginating this connection. </summary>
    public PageInfo_ PageInfo;
  }

}
