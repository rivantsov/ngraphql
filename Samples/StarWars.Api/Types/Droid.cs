using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>An autonomous mechanical character in the Star Wars universe </summary>
  [GraphQLObjectType("Droid")]
  public class Droid_ : ICharacter_ {
    /// <summary>The ID of the droid</summary>
    [Scalar("ID")]
    public string ID { get; set; }

    /// <summary>What others call this droid</summary>
    public string Name { get; set; }

    /// <summary>This droid&apos;s friends, or an empty list if they have none</summary>
    public IList<ICharacter_> Friends { get; }

    /// <summary>The friends of the droid exposed as a connection with edges</summary>
    [GraphQLName("friendsConnection")]
    public FriendsConnection_ GetFriendsConnection(int first, [Scalar("ID")] string after) { return default; }

    /// <summary>The movies this droid appears in</summary>
    public Episode AppearsIn { get; }

    /// <summary>This droid&apos;s primary function</summary>
    public string PrimaryFunction; //note: we can use field when member is not part of interface
  }

}
