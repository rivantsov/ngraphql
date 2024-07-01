using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
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

    hubConn.On<string, string>("ReceiveMessage", (user, message) =>
    {
      var encodedMsg = $"{user}: {message}";
      messages.Add(encodedMsg);
    });
    await hubConn.StartAsync();

    // send a few messages 
    await hubConn.SendAsync("SendMessage", "John", "Message 1");
    await hubConn.SendAsync("SendMessage", "John", "Message 2");
    await Task.Delay(500);

    Assert.AreEqual(2, messages.Count, "Expected messages");

  }// method

}
