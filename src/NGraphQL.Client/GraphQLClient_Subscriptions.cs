using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using NGraphQL.Subscriptions;
using System.Threading.Tasks;
using System.Threading;
using NGraphQL.Json;
using System.Text.Json;
using System.Linq;

namespace NGraphQL.Client;
using TDict = Dictionary<string, object>;

public partial class GraphQLClient {
  // _subscriptions
  List<ClientSubscription> _subscriptions = new();
  HubConnection _hubConnection;

  public void InitSubscriptions() {
    if (_hubConnection != null)
      return;
    var hubUrl = this._endpointUrl + "/subscriptions";
    _hubConnection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
    _hubConnection.On<string>(SubscriptionMethodNames.ClientReceiveMethod,
      (json) => { HandleReceivedHubMessage(json); });
    //Task.Run(async () => await _hubConnection.StartAsync());
    _hubConnection.StartAsync().Wait();
    Thread.Yield();
  }

  public async Task<ClientSubscription> Subscribe<TPayload>(string requestText, TDict vars, 
                                     Action<ClientSubscription, TPayload> action, string id = null) {
    id ??= $"Subscription/{Guid.NewGuid()}";
    var subInfo = new ClientSubscription() { Request = requestText, Variables = vars,  
               PayloadType = typeof(TPayload), Id = id
     };
    subInfo.OnReceived = (subInfo, payload) => {
      action(subInfo, (TPayload)payload);
    };
    _subscriptions.Add(subInfo);
    var subscribeMsg = new SubscribeMessage(subInfo.Id,  
              new SubscribePayload() { Query = requestText, Variables = vars});
    var msgJson = SerializationHelper.Serialize(subscribeMsg);
    await _hubConnection.SendAsync(SubscriptionMethodNames.ServerReceiveMethod, msgJson);
    return subInfo;
  }

  private void HandleReceivedHubMessage(string json) {
    var msg = SerializationHelper.DeserializePartial<PayloadMessage>(json);
    switch (msg.Type) {
      case "next":
        HandleHubNextMessage(msg);
        break;
    }
  }

  private void HandleHubNextMessage(PayloadMessage msg) {
    var clientSub = _subscriptions.FirstOrDefault(s => s.Id == msg.Id);
    if (clientSub == null)
      return;
    var ploadElem = (JsonElement)msg.Payload;
    object payload = null;
    if (ploadElem.ValueKind != JsonValueKind.Null && ploadElem.ValueKind != JsonValueKind.Undefined)
      payload = ploadElem.Deserialize(clientSub.PayloadType);
    clientSub.OnReceived(clientSub, payload);    
  }


}
