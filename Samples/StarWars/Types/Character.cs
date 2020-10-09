using System;
using System.Collections.Generic;
using System.Text;

namespace StarWars {

  public class Character: NamedObject {
    public IList<Character> Friends = new List<Character>();
    public IList<Episode> AppearsIn;
    public IList<string> FriendIds;
  }
}
