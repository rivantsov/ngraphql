using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NGraphQL.Server.Subscriptions {

  public class SubscriptionManager : ISubscriptionManager {
    public IMessageSender Sender { get; set; }

    public SubscriptionManager(IMessageSender sender) {
      Sender = sender;
    }

    public async Task MessageReceived(string client, string message) {
      await Sender.Broadcast(null, message); 
      // await Task.CompletedTask;
    }
  }
}
