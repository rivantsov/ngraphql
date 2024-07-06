using System;
using System.Collections.Generic;
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
    public string Id;
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

    // subscribe
    var client = TestEnv.Client;
    client.InitSubscriptions();
    var thingId = 1;
    var vars = new Dictionary<string, object>() { { "thingId", thingId } };
    await client.Subscribe<ThingUpdate>(subscribeRequest, vars, (clientSub, msg) => {
      updates.Add(msg);
    });

    // 2.  make Thing update through mutation
    await MutateThing(1, "newName");
    WaitYield();

    // 3. Check notifications pushed by server
    Assert.AreEqual(1, updates.Count, "Expected messages");

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
