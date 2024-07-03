using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Server.AspNetCore;

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

    // setup client
    var hubUrl = TestEnv.ServiceUrl + "/subscriptions";
    var hubConn = new HubConnectionBuilder().WithUrl(hubUrl).Build();
    hubConn.On<string>(SignalRNames.ClientReceiveMethod, (msg) => { messages.Add(msg);  });
    await hubConn.StartAsync();

    var serverMethod = SignalRNames.ServerReceiveMethod;
    await hubConn.SendAsync(serverMethod, "message 1");
    await hubConn.SendAsync(serverMethod, "message 2");
    // make multiple delays (for thread yields)
    for(int i=0; i < 5; i++) {
      Thread.Yield();
      await Task.Delay(20);
    }

    Assert.AreEqual(2, messages.Count, "Expected messages");

  }// method

}
