using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Things.GraphQL.HttpServer;

public class SampleSubscriptionHub : Hub {

  public async Task SendMessage(string user, string message) {
    await Clients.All.SendAsync("ReceiveMessage", user, message);
  }
}
