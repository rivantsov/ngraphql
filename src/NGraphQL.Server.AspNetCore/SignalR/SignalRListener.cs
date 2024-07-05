using System;
using System.Collections;
using System.Collections.Generic;
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
  public const string ServerReceiveMethod = nameof(ServerReceiveMessage);
  GraphQLServer _server;

  // The sender parameter is to just force DI to create the sender before calling this method
  public SignalRListener(GraphQLServer server, IMessageSender sender) {
    _server = server;
  }

  public override Task OnConnectedAsync() {
    _server.Subscriptions.OnClientConnected(Context.ConnectionId, Context.User, Context.UserIdentifier);
    return base.OnConnectedAsync();
  }
  public override Task OnDisconnectedAsync(Exception exception) {
    _server.Subscriptions.OnClientDisconnected(Context.ConnectionId, exception);
    return base.OnDisconnectedAsync(exception);
  }

  public async Task ServerReceiveMessage(string message) {
    await _server.Subscriptions.MessageReceived(this.Context.ConnectionId, message);
  }
}

