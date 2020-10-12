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

      query = @"
{
  reviews( episode: JEDI) { episode, stars, commentary, emojis }
}";
      resp = await TestEnv.ExecuteAsync(query, throwOnError: false);
      var reviews = resp.Data.GetValue<IList>("reviews");
      Assert.IsTrue(reviews.Count > 0, "Expected some review");
    }
  }
}
