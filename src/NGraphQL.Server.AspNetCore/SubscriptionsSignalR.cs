using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using NGraphQL.Server.Subscriptions;
using NGraphQL.Subscriptions;

namespace NGraphQL.Server.AspNetCore;

/// <summary> SignalR hub for receiving web socket messages for GraphQL subscriptions. </summary>
/// <remarks>This class, like base Hub class from SignalR, is transient. 
/// Instances are created to receive a single message and then disposed. </remarks>
public class SignalRListener: Hub {
  GraphQLServer _server;

  // The sender parameter is to just force DI to create the sender before calling this method
  public SignalRListener(GraphQLServer server, IMessageSender sender) {
    _server = server;
  }

  public async Task ReceiveMessage(string message) {
    var clientInfo = new SubscriptionClientInfo() {
      User = this.Context.User, UserIdentifier = this.Context.UserIdentifier, ConnectionId = this.Context.ConnectionId,
      Context = this.Context
    };
    await _server.Subscriptions.MessageReceived(clientInfo, message);
  }
}

/// <summary> Singleton service in charge of pushing messages to clients through Web sockets/SignalR. </summary>
public class SignalRSender: IMessageSender {

  IHubContext<SignalRListener> _hubContext;
  GraphQLServer _server;

  // parameters are injected by DI
  public SignalRSender(GraphQLServer server, IHubContext<SignalRListener> hubContext) {
    _server = server;
    _hubContext = hubContext;
    _server.Subscriptions.Init(this); 
  }

  public async Task SendMessage(string subscriber, string message) {
    await _hubContext.Clients.Client(subscriber).SendAsync(SignalRNames.ClientReceiveMethod, message);
  }

  public async Task Broadcast(string topic, string message) {
    var clientProxy = string.IsNullOrEmpty(topic) ?
        _hubContext.Clients.All : _hubContext.Clients.Group(topic);
    await clientProxy?.SendAsync(SignalRNames.ClientReceiveMethod, message);
  }

  public async Task Subscribe(string topic, string subscriber) {
    await _hubContext.Groups.AddToGroupAsync(subscriber, topic);
  }

  public async Task Unsubscribe(string topic, string subscriber) {
    await _hubContext.Groups.RemoveFromGroupAsync(subscriber, topic);
  }

}

public static class SignalRNames {
  public const string ClientReceiveMethod = "ReceiveMessage";
  public const string ServerReceiveMethod = nameof(SignalRListener.ReceiveMessage);

}



