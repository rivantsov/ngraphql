using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using NGraphQL.Server.AspNetCore;
using NGraphQL.Subscriptions;
using Things.GraphQL.Types;

namespace NGraphQL.Tests.HttpTests;

[TestClass]
public class SubscriptionTests {

  [TestInitialize]
  public void Init() {
    TestEnv.Initialize();
  }

  [TestMethod]
  public async Task TestSimpleSubscription() {
    TestEnv.LogTestMethodStart();
    TestEnv.LogTestDescr(@"  Simple subscription test");
    var messages = new List<string>();

    var hubUrl = TestEnv.ServiceUrl + "/subscriptions";
    var hubConn = new HubConnectionBuilder().WithUrl(hubUrl).Build();


    hubConn.On<string>("ReceiveMessage", (msg) =>
    {
      messages.Add(msg);
    });
    await hubConn.StartAsync();

    var serverMethod = SubscriptionHub.ReceiveMethodName;
    await hubConn.SendAsync(serverMethod, "message 1");
    await hubConn.SendAsync(serverMethod, "message 2");
    await Task.Delay(500);

    Assert.AreEqual(2, messages.Count, "Expected messages");

  }// method

}
