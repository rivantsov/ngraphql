using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NGraphQL.Server.Subscriptions; 

public interface IMessageSender {
  Task SendMessage(string connectionId, string message);
  Task Broadcast(string topic, string message);
  Task Subscribe(string topic, string connectionId);
  Task Unsubscribe(string topic, string connectionId);
}


