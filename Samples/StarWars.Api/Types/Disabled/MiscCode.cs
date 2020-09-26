using System;
using System.Collections.Generic;
using System.Text;

namespace StarWars.Api.Types {
  class MiscCode {
    public FriendsConnection_ GetFriendsConnection(IFieldContext fieldContext, Character character, int first, string after)
    {
      var allFriends = character.Friends;
      int skip = 0;
      if (!string.IsNullOrEmpty(after))
      {
        skip = allFriends.GetIndexOf(f => f.ID == after);
        if (skip < 0) //not found
          skip = 0; //reset
      }
      var friendsPlusOne = allFriends.Skip(skip).Take(first + 1).ToList();
      var hasNextPage = friendsPlusOne.Count > first;
      var friends = friendsPlusOne.Take(first).ToList();
      var friendsEdges = friends.Select(f => new FriendsEdge_() { Cursor = f.ID, Node = new InterfaceBox<ICharacter_>(f) }).ToList();
      var fConn = new FriendsConnection_() { TotalCount = allFriends.Count, Friends = friendsEdges };
      if (friends.Count > 0)
        fConn.PageInfo = new PageInfo_() { StartCursor = friends.First().ID, EndCursor = friends.Last().ID, HasNextPage = hasNextPage };
      else
        fConn.PageInfo = new PageInfo_();
      return fConn;
    }



  }
}
