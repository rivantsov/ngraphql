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

  public async Task InitSubscriptions(string hubUrl = "graphql/subscriptions") {
    if (_hubConnection != null)
      return;
    _hubConnection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
    _hubConnection.On<string>(SubscriptionMethodNames.ClientReceiveMethod,
      (json) => { HandleReceivedMessage(json); });
    await _hubConnection.StartAsync();
  }

  string _machineName = Environment.MachineName;
  int _subCount = 0;

  public async Task<ClientSubscription> Subscribe<TPayload>(string requestText, TDict vars,
                                     Action<ClientSubscription, TPayload> action,
                                     Action<ClientSubscription, ErrorMessage> errorAction = null,
                                     string id = null) {
    try {
      return await SubscribeImpl<TPayload>(requestText, vars, action, errorAction, id);
    } catch (Exception exc) {
      var info = "Subscription request: " + requestText;
      this.OnError?.Invoke(this, new Types.RequestErrorEventArgs(exc, info));
      throw;
    }
  }

  public ClientSubscription[] GetSubscriptions() => _subscriptions.ToArray();
  public HubConnection GetHub() => _hubConnection;


  public async Task Unsubscribe(string subscriptionId) {
    var sub = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
    if (sub == null)
      throw new Exception("Subscription not found.");
    await Unsubscribe(sub); 
  }

  public async Task Unsubscribe(ClientSubscription sub) {
    if (!_subscriptions.Contains(sub))
      throw new Exception("Subscription not registered.");
    var completeMsg = new CompleteMessage() { Id = sub.Id };
    var msgJson = SerializationHelper.Serialize(completeMsg);
    await _hubConnection.SendAsync(SubscriptionMethodNames.ServerReceiveMethod, msgJson);
    _subscriptions.Remove(sub);      
  }

  public async Task PingServer() {
    var msg = new PingMessage();
    var msgJson = SerializationHelper.Serialize(msg);
    await _hubConnection.SendAsync(SubscriptionMethodNames.ServerReceiveMethod, msgJson);
  }

  private async Task<ClientSubscription> SubscribeImpl<TPayload>(string requestText, TDict vars, 
                                     Action<ClientSubscription, TPayload> action, 
                                     Action<ClientSubscription, ErrorMessage> errorAction = null,  
                                     string id = null) {
    id ??= $"{_machineName}/{_subCount++}";
    var subscr = new ClientSubscription() { Request = requestText, Variables = vars,  
               PayloadType = typeof(TPayload), OnErrorAction = errorAction, Id = id
     };
    subscr.OnReceiveAction = (subInfo, payload) => {
      action(subInfo, (TPayload)payload);
    };
    _subscriptions.Add(subscr);
    var subscribeMsg = new SubscribeMessage(subscr.Id,  
              new SubscribePayload() { Query = requestText, Variables = vars});
    var msgJson = SerializationHelper.Serialize(subscribeMsg);
    await _hubConnection.SendAsync(SubscriptionMethodNames.ServerReceiveMethod, msgJson);
    return subscr;
  }

  private void HandleReceivedMessage(string json) {
    // generally it is message without errors, but this does not hurt
    var msg = SerializationHelper.DeserializePartial<PayloadMessage>(json);
    MessageReceived?.Invoke(this, new Types.SubscriptionMessageEventArgs(msg));
    switch (msg.Type) {
      case SubscriptionMessageTypes.Next:
        HandleReceivedNextMessage(msg);
        break;
      case SubscriptionMessageTypes.Error:
        HandleReceivedErrorMessage(msg); 
        break;
      case SubscriptionMessageTypes.Pong: //nothing to do
        break;
      case SubscriptionMessageTypes.ConnectionAck: //nothing to do
        break;
    }
  }

  private void HandleReceivedNextMessage(PayloadMessage msg) {
    var clientSub = _subscriptions.FirstOrDefault(s => s.Id == msg.Id);
    if (clientSub == null)
      return;
    var ploadElem = (JsonElement)msg.Payload;
    object payload = null;
    if (ploadElem.ValueKind != JsonValueKind.Null && ploadElem.ValueKind != JsonValueKind.Undefined)
      payload = ploadElem.Deserialize(clientSub.PayloadType, JsonDefaults.JsonOptions);
    clientSub.OnReceiveAction(clientSub, payload);    
  }

  private void HandleReceivedErrorMessage(PayloadMessage msg) {
    var clientSub = _subscriptions.FirstOrDefault(s => s.Id == msg.Id);
    if (clientSub == null)
      return;
    var ploadElem = (JsonElement)msg.Payload;
    GraphQLError[] errors = null;
    if (ploadElem.ValueKind != JsonValueKind.Null && ploadElem.ValueKind != JsonValueKind.Undefined)
      errors = ploadElem.Deserialize<GraphQLError[]>( JsonDefaults.JsonOptions);
    var errMsg = new ErrorMessage() { Id = msg.Id, Payload = errors };
    this.ErrorReceived?.Invoke(this, new Types.ErrorMessageEventArgs(errMsg));
    clientSub.OnErrorAction?.Invoke(clientSub, errMsg);
  }

}
