using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using NGraphQL.Server.Subscriptions;

namespace NGraphQL.Server.AspNetCore;

/// <summary> SignalR hub for receiving web socket messages for GraphQL subscriptions. </summary>
/// <remarks>This class, like base Hub class from SignalR, is transient, 
/// meaning instances are created to receive a single message and then disposed. </remarks>
public class SignalRListener: Hub {
  public const string ServerReceiveMethod = nameof(ReceiveMessage);
  SubscriptionManager _manager;

  public SignalRListener(SubscriptionManager manager) {
    _manager = manager;
  }

  public async Task ReceiveMessage(string message) {
    await _manager.MessageReceived(this.Context.ConnectionId, message);
  }
}

public class SignalRSender: IMessageSender {
  public const string ClientReceiveMethod = "ReceiveMessage";

  IHubContext<SignalRListener> _hubContext;

  // parameters are injected by DI
  public SignalRSender(IHubContext<SignalRListener> hubContext) {
    _hubContext = hubContext;
  }

  public async Task Broadcast(string subject, string message) {
    var clientProxy = string.IsNullOrEmpty(subject) ?
        _hubContext.Clients.All : _hubContext.Clients.Group(subject);
    await clientProxy?.SendAsync(ClientReceiveMethod, message);
  }

  public async Task SendMessage(string subscriber, string message) {
    await _hubContext.Clients.Client(subscriber).SendAsync(ClientReceiveMethod, message);
  }

  public async Task Subscribe(string subscriber, string subject) {
    await _hubContext.Groups.AddToGroupAsync(subscriber, subject);
  }

  public async Task Unsubscribe(string subscriber, string subject) {
    await _hubContext.Groups.RemoveFromGroupAsync(subscriber, subject);
  }

  //public async Task SendMessage(string group, string message) {
  //    var grp = _hubContext.Clients.Group(group);
  //    await  grp.SendAsync("ReceiveMessage", message);
  //}

}

