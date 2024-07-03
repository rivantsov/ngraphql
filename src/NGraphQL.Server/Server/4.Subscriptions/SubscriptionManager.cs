using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NGraphQL.Server.Subscriptions {

  public class SubscriptionManager {
    IMessageSender _sender;

    public SubscriptionManager(IMessageSender sender) {
      _sender = sender;
    }

    public async Task MessageReceived(string client, string message) {
      await _sender.Broadcast(null, "Server: " + message); 
      // await Task.CompletedTask;
    }
  }
}
