﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace StarWars {
  using FriendLink = Tuple<string, string>;
  using PilotLink = Tuple<string, string>;

  public partial class StarWarsApp {
    // Base data
    public List<Character> Characters = new List<Character>();
    public List<Starship> Starships = new List<Starship>();
    public List<Review> Reviews = new List<Review>();
    
    public StarWarsApp() {
      InitData(); 
    }

    public Episode GetAllEpisodes() {
      return Episode.Newhope | Episode.Empire | Episode.Jedi;
    }

    public IList<Starship> GetStarships() {
      return Starships;
    }

    public Starship GetStarship(string id) {
      return Starships.FirstOrDefault(s => s.Id == id);
    }

    public IList<Character> GetCharacters(Episode episode) {
      return Characters.Where(c => c.AppearsIn.Includes(episode)).ToList();
    }

    public Character GetCharacter(string id) {
      return Characters.FirstOrDefault(c => c.Id == id);
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

    // mutation CreateReview
    public Review CreateReview(Episode episode, int stars, string commentary) {
      var review = new Review() { Episode = episode, Stars = stars, Commentary = commentary };
      Reviews.Add(review);
      return review; 
    }

    // used by Friends field resolver in batch mode (aka DataLoader)
    // return dictionary of characters with lists of their friends
    public IDictionary<Character, IList<Character>> GetFriendLists(IList<Character> characters) {
      var result = new Dictionary<Character, IList<Character>>();
      foreach(var character in characters) {
        var friends = this.Characters.Where(c => character.FriendIds.Contains(c.Id)).ToList();
        result[character] = friends;
      }
      return result; 
    }

  }
}