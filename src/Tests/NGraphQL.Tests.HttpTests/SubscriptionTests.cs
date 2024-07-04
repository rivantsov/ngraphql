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

namespace NGraphQL.Tests.HttpTests;

[TestClass]
public class SubscriptionTests {

  [TestInitialize]
  public void Init() {
    TestEnv.Initialize();
  }

  [TestMethod]
  public async Task TestSubscriptions() {
    TestEnv.LogTestMethodStart();
    TestEnv.LogTestDescr(@"  Simple subscription test");
    var messages = new List<string>();

    // setup SignalR client
    var hubUrl = TestEnv.ServiceUrl + "/subscriptions";
    var hubConn = new HubConnectionBuilder().WithUrl(hubUrl).Build();
    hubConn.On<string>(SignalRNames.ClientReceiveMethod, 
      (msg) => { 
        messages.Add(msg);  
    });
    await hubConn.StartAsync();

    // 1. AddSubscription to ThingUpdates
    var thingId = 1;
    var subscribeMsg = new SubscribeMessage() {
      Id = "ThingUpdate/1/" + Guid.NewGuid(),
      Type = SubscriptionMessageTypes.Subscribe,
      Payload = new SubscribePayload() {
        OperationName = null,
        Query = @"
subscription($thingId: Int) {
  subscribeToThingUpdates(thingId: $thingId) {
     id name kind 
  }
}",
        Variables = new Dictionary<string, object>() { { "thingId", thingId } },
       }
    };

    var msgJson = SerializationHelper.Serialize(subscribeMsg);
    var serverMethod = SignalRNames.ServerReceiveMethod;
    await hubConn.SendAsync(serverMethod, msgJson);
    
    
    // make multiple delays (for thread yields)
    for(int i=0; i < 5; i++) {
      Thread.Yield();
      await Task.Delay(20);
    }

    Assert.AreEqual(2, messages.Count, "Expected messages");

  }// method

}
