using System;
using System.Collections.Generic;
using System.Linq;

namespace StarWars {

  public class StarWarsApp {
    // Base data
    public IList<Character> Characters = new List<Character>();
    public IList<Starship> Starships = new List<Starship>();
    public IList<Review> Reviews = new List<Review>(); 

    // Derived collections
    public IEnumerable<Human> Humans => Characters.OfType<Human>();
    public IEnumerable<Droid> Droids => Characters.OfType<Droid>();   

    // transactions mock
    public Transaction BeginTransaction() {
      return new Transaction(); 
    }

    // methods supporting API
    public Character GetHeroOf(Episode episode) {
      // find the first character that was in this episode
      return Characters.FirstOrDefault(c => c.AppearsIn.Includes(episode));
    }

    public IList<Review> GetReviews(Episode episode) {
      return Reviews.Where(r => r.Episode == episode).ToList(); 
    }

    public IEnumerable<NamedObject> Search(string text) {
      // check characters and starships
      foreach (var ch in Characters)
        if (ch.Name.Contains(text))
          yield return ch;
      foreach (var sh in Starships)
        if (sh.Name.Contains(text))
          yield return sh; 
    }

    public Character GetCharacter(string id) {
      return Characters.FirstOrDefault(c => c.ID == id);
    }
    public Human GetHuman(string id) {
      return Humans.FirstOrDefault(h => h.ID == id);
    }
    public Droid GetDroid(string id) {
      return Droids.FirstOrDefault(d => d.ID == id);
    }
    public Starship GetStarship(string id) {
      return Starships.FirstOrDefault(s => s.ID == id);
    }

    // mutation CreateReview
    public Review CreateReview(Episode episode, int stars, string commentary) {
      var review = new Review() { Episode = episode, Stars = stars, Commentary = commentary };
      Reviews.Add(review);
      return review; 
    }

  }
}
