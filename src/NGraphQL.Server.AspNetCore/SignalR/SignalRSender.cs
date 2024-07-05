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

/// <summary> Singleton service in charge of pushing messages to clients through Web sockets/SignalR. </summary>
public class SignalRSender: IMessageSender {
  public const string ClientReceiveMethod = "ClientReceiveMessage";

  IHubContext<SignalRListener> _hubContext;
  GraphQLServer _server;

  // parameters are injected by DI
  public SignalRSender(GraphQLServer server, IHubContext<SignalRListener> hubContext) {
    _server = server;
    _hubContext = hubContext;
    _server.Subscriptions.Init(this); 
  }

  public async Task Publish(string message, IList<string> connectionIds) {
    foreach (var conn in connectionIds)
      await PushMessage(message, conn);
  }
  public async Task PushMessage(string message, string connectionId) {
    await _hubContext.Clients.Client(connectionId).SendAsync(SignalRNames.ClientReceiveMethod, message);
  }


  public async Task Subscribe(string group, string connectionId) {
    await _hubContext.Groups.AddToGroupAsync(connectionId, group);
  }

  public async Task Unsubscribe(string group, string connectionId) {
    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, group);
  }

}

public static class SignalRNames {
  public const string ClientReceiveMethod = "ClientReceiveMessage";
  public const string ServerReceiveMethod = nameof(SignalRListener.ServerReceiveMessage);

}



