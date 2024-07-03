using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using NGraphQL.Subscriptions;

namespace NGraphQL.Server.AspNetCore;

public class SubscriptionHub : Hub {
  public const string ReceiveMethodName = nameof(ReceiveMessage);
  SubscriptionManager _manager;

  public SubscriptionHub(SubscriptionManager manager) {
    _manager = manager;
  }

  public async Task ReceiveMessage(string json) {
    await _manager.ReceiveMessage(this.Context, json);
  }
}

public class SubscriptionManager {
  IHubContext<SubscriptionHub> _hubContext;

  public SubscriptionManager(IHubContext<SubscriptionHub> hubContext) {
      _hubContext = hubContext; 
  }
  public async Task ReceiveMessage(HubCallerContext context, string message) {
    //JsonSerializer.DeserializeAsync<MessageBase>().
    Trace.WriteLine(" received message: " + message);
    await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
    await Task.CompletedTask;
  }

    public async Task SendMessage(string group, string message) {
      var grp = _hubContext.Clients.Group(group);
      await  grp.SendAsync("ReceiveMessage", message);
    }
}

