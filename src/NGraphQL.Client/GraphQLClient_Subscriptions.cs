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
public partial class GraphQLClient {
  // _subscriptions
  List<ClientSubscription> _subscriptions = new();
  HubConnection _hubConnection;

  private void InitSubscriptions() {
    if (_hubConnection != null)
      return;
    var hubUrl = this._endpointUrl + "/subscriptions";
    _hubConnection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
    _hubConnection.On<string>(SubscriptionMethodNames.ClientReceiveMethod,
      (json) => { HandleReceivedHubMessage(json); });
    Task.Run(async () => await _hubConnection.StartAsync());
    Thread.Yield();
  }

  public ClientSubscription Subscribe<TPayload>(string requestText, string topic,
                                     Action<ClientSubscription, TPayload> action) {
    var subInfo = new ClientSubscription() { Request = requestText, Topic = topic, PayloadType = typeof(TPayload) };
    subInfo.OnReceived = (subInfo, payload) => {
      action(subInfo, (TPayload)payload);
    };
    _subscriptions.Add(subInfo);
    return subInfo;
  }

  private void HandleReceivedHubMessage(string json) {
    var msg = SerializationHelper.DeserializePartial<PayloadMessage>(json);
    switch (msg.Type) {
      case "next":
        HandleSubscriptionNextMessage(msg);
        break;

    }
  }

  private void HandleSubscriptionNextMessage(PayloadMessage msg) {
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
