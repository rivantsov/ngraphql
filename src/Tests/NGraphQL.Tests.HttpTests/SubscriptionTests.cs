using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Json;
using NGraphQL.Server.AspNetCore;
using NGraphQL.Subscriptions;
using Things;

namespace NGraphQL.Tests.HttpTests;
using TVars = Dictionary<string, object>;

[TestClass]
public class SubscriptionTests {

  [TestInitialize]
  public void Init() {
    TestEnv.Initialize();
  }

  public class ThingUpdate {
    public int Id;
    public string Name;
    public ThingKind Kind; 
  }

  [TestMethod]
  public async Task TestSubscriptions() {
    TestEnv.LogTestMethodStart();
    TestEnv.LogTestDescr(@"  Simple subscription test");
    var updates = new List<ThingUpdate>();

    const string subscribeRequest = @"
subscription($thingId: Int) {
  subscribeToThingUpdates(thingId: $thingId) {
     id name kind 
  }
}";

    // 1. Subscribe to updates of Thing #1 and #2
    var client = TestEnv.Client;
    await client.Subscribe<ThingUpdate>(subscribeRequest, new TVars() { { "thingId", 1 } }, (clientSub, msg) => {
      updates.Add(msg);
    });
    await client.Subscribe<ThingUpdate>(subscribeRequest, new TVars() { { "thingId", 2 } }, (clientSub, msg) => {
      updates.Add(msg);
    });
    WaitYield();

    // 2.Make Thing update through mutation
    await MutateThing(1, "newName_1A");
    await MutateThing(1, "newName_1B");
    await MutateThing(2, "newName_2A");
    await MutateThing(3, "newName_3A"); //this will not come, we are not subscribed to #3
    WaitYield();

    // 3. Check notifications pushed by the server
    Assert.AreEqual(3, updates.Count, "Expected 3 total notifications");
    var updates1 = updates.Where(u => u.Id == 1).ToList();
    var updates2 = updates.Where(u => u.Id == 2).ToList();
    Assert.AreEqual(2, updates1.Count, "Expected 2 updates for Thing 1");
    Assert.AreEqual(1, updates2.Count, "Expected 1 update for Thing 2");


  }// method

  private void WaitYield() {
    for (int i = 0; i < 3; i++) {
      Thread.Sleep(20);
    }
  }

  private async Task MutateThing(int thingId, string newName) {
    var mutReq = @"
mutation myMut($thingId: Int, $newName: String) { 
  mutateThing(id: $thingId, newName: $newName) { 
    id 
    name 
  }
}";
    var vars = new TVars() { { "thingId", thingId }, { "newName", newName } };
    await TestEnv.Client.PostAsync(mutReq, vars);
  }

}
