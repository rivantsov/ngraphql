using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace StarWars {
  public partial class StarWarsApp {

    private void InitData() {
      var allEpisodes = new[] { Episode.Empire, Episode.Jedi, Episode.Newhope };
      // Humans
      var luke = new Human() { Id = "1000", Name = "Luke Skywalker", AppearsIn = allEpisodes, HomePlanet = "Tatooine",
                 Height = 1.72f, MassKg = 77,
        StarshipIds = ToList("3001", "3003"),
        FriendIds = ToList("1002", "1003", "2000", "2001"),
      };

      var darth = new Human() { Id = "1001", Name = "Darth Vader", AppearsIn =  allEpisodes, HomePlanet = "Tatooine",
                 Height = 2.02f, MassKg = 136,
        FriendIds = ToList("1004"),
        StarshipIds = ToList("3002")
      };
      var han = new Human() { Id = "1002", Name = "Han Solo", AppearsIn = allEpisodes, Height = 1.8f, MassKg = 80,
        FriendIds = ToList( "1000", "1003", "2001"),
        StarshipIds = ToList("3000", "3003"),
      };
      var leia = new Human() { Id = "1003", Name = "Leia Organa", AppearsIn = allEpisodes, HomePlanet = "Alderaan",
                               Height = 1.5f, MassKg = 49,
        FriendIds = ToList("1000", "1002", "2000", "2001"),
        StarshipIds = ToList(),
      };
      var wilhuff = new Human() { Id = "1004", Name = "Wilhuff Tarkin", AppearsIn = new[] { Episode.Newhope }, Height = 1.8f,
        //starships:[],
        // friends:[ "1001" ],
      };

      //Droids
      var c3po = new Droid() { Id = "2000", Name = "C-3PO", AppearsIn = allEpisodes,
        PrimaryFunction = "Protocol",
        // friends:[ "1000", "1002", "1003", "2001" ],
      };
      var r2 = new Droid() { Id = "2001", Name = "R2-D2", AppearsIn = allEpisodes, PrimaryFunction = "Astromech",
        //Friends = [ "1000", "1002", "1003" ],
      };

      Characters.AddRange(new Character[] { luke, darth, han, leia, wilhuff, c3po, r2 });

      // Starships
      var s0 = new Starship() { Id = "3000", Name = "Millenium Falcon", Length = 34.37f };
      var s1 = new Starship() { Id = "3001", Name = "X-Wing", Length = 12.5f };
      var s2 = new Starship() { Id = "3002", Name = "TIE Advanced x1", Length = 9.2f };
      var s3 = new Starship() { Id = "3003", Name = "Imperial shuttle", Length = 20 };
      Starships.AddRange(new[] { s0, s1, s2, s3 });

    }// method

    private List<string> ToList(params string[] strings) => strings?.ToList() ?? new List<string>(); 

  }
}
