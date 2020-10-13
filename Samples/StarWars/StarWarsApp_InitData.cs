using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace StarWars {
  public partial class StarWarsApp {

    private void InitData() {
      // Starships
      var s0 = new Starship() { Id = "3000", Name = "Millenium Falcon", Length = 34.37f };
      var s1 = new Starship() { Id = "3001", Name = "X-Wing", Length = 12.5f };
      var s2 = new Starship() { Id = "3002", Name = "TIE Advanced x1", Length = 9.2f };
      var s3 = new Starship() { Id = "3003", Name = "Imperial shuttle", Length = 20 };
      Starships.AddRange(new[] { s0, s1, s2, s3 });

      var allEpisodes = new[] { Episode.Empire, Episode.Jedi, Episode.Newhope };
      // Humans
      var luke = new Human() { Id = "1000", Name = "Luke Skywalker", AppearsIn = allEpisodes, HomePlanet = "Tatooine",
        Height = 1.72f, MassKg = 77,
        Starships = new[] { s0, s3 }
      };

      var darth = new Human() { Id = "1001", Name = "Darth Vader", AppearsIn = allEpisodes, HomePlanet = "Tatooine",
        Height = 2.02f, MassKg = 136,
        Starships = new[] { s2 }
      };
      var han = new Human() { Id = "1002", Name = "Han Solo", AppearsIn = allEpisodes, Height = 1.8f, MassKg = 80,
        Starships = new[] { s0, s3 }
      };
      var leia = new Human() { Id = "1003", Name = "Leia Organa", AppearsIn = allEpisodes, HomePlanet = "Alderaan",
        Height = 1.5f, MassKg = 49,
        Starships = new Starship[] {}
      };
      var wilhuff = new Human() { Id = "1004", Name = "Wilhuff Tarkin", AppearsIn = new[] { Episode.Newhope }, Height = 1.8f,
        Starships = new Starship[] { }
      };

      //Droids
      var c3po = new Droid() { Id = "2000", Name = "C-3PO", AppearsIn = allEpisodes,
        PrimaryFunction = "Protocol",
      };
      var r2 = new Droid() { Id = "2001", Name = "R2-D2", AppearsIn = allEpisodes, PrimaryFunction = "Astromech",
      };

      // friends
      luke.Friends = new Character[] { han, leia, c3po, r2 };
      darth.Friends = new Character[] { wilhuff };
      han.Friends = new Character[] { luke, leia, r2 };
      leia.Friends = new Character[] { luke, han, c3po, r2 };
      wilhuff.Friends = new Character[] { darth };
      c3po.Friends = new Character[] { luke, leia, r2 };
      r2.Friends = new Character[] { luke, han, leia, c3po };

      Characters.AddRange(new Character[] { luke, darth, han, leia, wilhuff, c3po, r2 });

      // reviews
      CreateReview(Episode.Empire, 2, "Booooring", Emojis.Bored | Emojis.Dislike);
      CreateReview(Episode.Jedi, 5, "Fabulous!", Emojis.Excited | Emojis.Like);
      CreateReview(Episode.Newhope, 4, "Could be better", Emojis.Like | Emojis.Smile);

    }// method

  }
}
