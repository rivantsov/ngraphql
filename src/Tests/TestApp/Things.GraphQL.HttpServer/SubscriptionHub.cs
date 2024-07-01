using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Things.GraphQL.HttpServer {
  public class SubscriptionHub : Hub {

    public async Task SendMessage(string user, string message) {
      await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
  }
}
