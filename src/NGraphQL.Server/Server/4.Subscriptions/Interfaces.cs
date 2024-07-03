using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NGraphQL.Server.Subscriptions; 

public interface IMessageSender {
  Task SendMessage(string subscriber, string message);
  Task Broadcast(string subject, string message);
  Task Subscribe(string subscriber, string subject);
  Task Unsubscribe(string subscriber, string subject);
}

public interface ISubscriptionManager {
  IMessageSender Sender { get; set; }
  Task MessageReceived(string client, string message);
}
