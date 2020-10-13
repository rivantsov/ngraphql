using System.Collections;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StarWars.Api.Tests;
using NGraphQL.Utilities;

namespace StarWars.Tests
{
  [TestClass]
  public class StarWarsTests
  {
    [TestInitialize]
    public void TestInit() {
      TestEnv.Init(); 
    }

    [TestMethod]
    public async Task TestStarWarsApi()
    {
      var query = @"
query {
  starships{name, length}
}
";
      var resp = await TestEnv.ExecuteAsync(query, throwOnError: false);
      Assert.IsNotNull(resp, "Expected response");
      Assert.AreEqual(0, resp.Errors.Count, "Expected no errors");
      var ships = resp.Data.GetValue<IList>("starships");
      Assert.AreEqual(4, ships.Count, "expected 4 ships");

      // friends query
      query = @"
{
  leia: character(id: ""1003"") { 
    name
    friends {
      name
    }
  }
}";
      resp = await TestEnv.ExecuteAsync(query, throwOnError: false);
      var leiaFriends = resp.Data.GetValue<IList>("leia.friends");
      Assert.AreEqual(4, leiaFriends.Count, "Expected 4 friends");

      // get Jedi reviews, add review, check new count
      // 1. Get Jedi reviews, get count
      var getReviewsQuery = @"
{
  reviews( episode: JEDI) { episode, stars, commentary, emojis }
}";
      resp = await TestEnv.ExecuteAsync(getReviewsQuery);
      var jediReviews = resp.Data.GetValue<IList>("reviews");
      Assert.IsTrue(jediReviews.Count > 0, "Expected some review");
      var oldReviewsCount = jediReviews.Count;

      // 2. Add review for Jedi
      var createReviewMut = @"
mutation {
  createReview( episode: JEDI, reviewInput: { stars: 5, commentary: ""very good"", emojis: [LIKE, EXCITED]}) 
    { episode, stars, commentary, emojis }
}";
      resp = await TestEnv.ExecuteAsync(createReviewMut);

      resp = await TestEnv.ExecuteAsync(getReviewsQuery);
      jediReviews = resp.Data.GetValue<IList>("reviews");
      var newReviewsCount = jediReviews.Count;
      Assert.AreEqual(oldReviewsCount + 1, newReviewsCount, "Expected incremented review count");

    }
  }
}
