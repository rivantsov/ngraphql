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

  IHubContext<SignalRListener> _hubContext;
  GraphQLServer _server;

  // parameters are injected by DI
  public SignalRSender(GraphQLServer server, IHubContext<SignalRListener> hubContext) {
    _server = server;
    _hubContext = hubContext;
    _server.Subscriptions.SetSender(this); 
  }

  public async Task Publish(string message, IList<string> connectionIds) {
    foreach (var conn in connectionIds)
      await PushMessage(message, conn);
  }

  public async Task PushMessage(string message, string connectionId) {
    await _hubContext.Clients.Client(connectionId).SendAsync(
             SubscriptionMethodNames.ClientReceiveMethod, message);
  }

}

