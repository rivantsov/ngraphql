using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Json;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Subscriptions;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Subscriptions;

public class SubscriptionManager {
  IMessageSender _sender;
  GraphQLServer _server;
  ClientSubscriptionStore _subscriptionStore = new(); 

  public SubscriptionManager(GraphQLServer server) {
    _server = server;
  }

  // provides a way for SignalRSender singleton to register with SubscriptionManager. 
  // It's a bit complicated, SignalRListener (hub, transient object) and SignalRSender are created by DI, late, when the first subscribe message is received.
  // It starts from Listener, it has Sender as parameter(not used) that forces DI to create the MessageSender instance (singleton),
  // and sender immediately registers itself with SubscriptionManager. 
  public void SetSender(IMessageSender sender) {
    if (_sender == null)
      _sender = sender; 
  }

  public void OnClientConnected(string connectionId, ClaimsPrincipal user, string userId) {
    var conn = new ClientConnection() { ConnectionId = connectionId, User = user, UserId = userId };
    _subscriptionStore.AddClient(conn); 
  }

  public void OnClientDisconnected(string connectionId, Exception exc) {
    _subscriptionStore.RemoveClient(connectionId);  
  }
     
  public async Task MessageReceived(string connectionId, string message) {
    try {
      Util.Check(_sender != null, "Subscription manager not initialized, message sender not set.");
      await MessageReceivedImpl(connectionId, message); 
    } catch(Exception ex) {
      Trace.WriteLine($"MessageReceived error: {ex.ToString()}");
      Debugger.Break(); 
    }
  }
  
  private async Task MessageReceivedImpl(string connectionId, string message) {
    var client = _subscriptionStore.GetClient(connectionId);
    if (client == null)
      return;
    var msg = SerializationHelper.DeserializePartial<PayloadMessage>(message); 
    switch(msg.Type) {
      case SubscriptionMessageTypes.Subscribe:
        await HandleSubscribeMessage(client, msg); 
        break;
      case SubscriptionMessageTypes.Complete:
        break;
      default:
        break;
    }
  }

  // Sequence: SignalRListener -> HandleSubscribeMessage -> ExecuteRequest -> Resolver -> AddSubscription(here)
  private async Task HandleSubscribeMessage(ClientConnection client, PayloadMessage message) {
    var payloadElem = (JsonElement)message.Payload;
    Util.Check(payloadElem.ValueKind == JsonValueKind.Object, "Subscribe.Payload is a JsonElement of invalid type {0}.", message.Type);
    var pload = payloadElem.Deserialize<SubscribePayload>(JsonDefaults.JsonOptions);
    // parse the query
    var rawReq = new GraphQLRequest() { OperationName = pload.OperationName, Query = pload.Query, Variables = pload.Variables };
    var requestContext = new RequestContext(this._server, rawReq, CancellationToken.None);
    requestContext.SubscriptionClient = client; 
    await _server.ExecuteRequestAsync(requestContext); //in the call, the resolver adds subscription using AddSubscription method below
  }

  // To be called by Subscription Resolver method
  public ClientSubscriptionInfo SubscribeCaller(IFieldContext field, string topic) {
    var reqContext = (RequestContext) field.RequestContext;
    var connId = reqContext.SubscriptionClient.ConnectionId;
    var client = _subscriptionStore.GetClient(connId);
    if (client == null)
      return null;
    var parsedReq = reqContext.ParsedRequest;
    var clientSub = _subscriptionStore.AddSubscription(client, topic, parsedReq);
    return clientSub;
  }

  public void UnsubscribeCaller(string topic, string connectionId) {
    _subscriptionStore.RemoveSubscription(topic, connectionId);
  }

  public async Task Publish(string topic, object payload) {
    //await PublishImpl(topic, payload);
    // Execute it on background thread, to avoid blocking caller for too long
    var task = Task.Run(async () => await PublishImpl(topic, payload));
    await Task.CompletedTask;
  }

  private async Task PublishImpl(string topic, object payload) {
    var subs = _subscriptionStore.GetTopicSubscriptions(topic);
    if (subs.Count == 0)
      return; 
    // Group by sub variant
    var varGroups = subs.GroupBy(sub => sub.Variant);
    // Each group contains Subscriptions with the same Variant (topic and query), so all clients will get
    // identical data 
    foreach(var grp in varGroups) {
      var subscrVariant = grp.Key;
      var msgJson = await BuildNotificationMessage(subscrVariant, payload);
      var clientSubs = grp.ToList();
      // TODO: format messages individually, including unique-id for each client
      var connIds = clientSubs.Select(cs => cs.Client.ConnectionId).ToList();
      await _sender.Publish(msgJson, connIds);
    }
  }

  private async Task<string> BuildNotificationMessage(SubscriptionVariant sub, object data) {
    var opId = sub.Topic; 
    try {
      var reqContext = new RequestContext(_server, sub.ParsedRequest, null);
      var reqHandler = new RequestHandler(_server, reqContext);
      var topOp = sub.ParsedRequest.Operations.First();
      var topScope = new OutputObjectScope(new RequestPath(), null, null) 
        { IsSubscriptionNextTopScope = true, SubscriptionNextResolverResult = data };
      await reqHandler.ExecuteOperationAsync(topOp, topScope);
      // top scope is similar to Data node in regular query; it contains top node corresponding 
      //  to subscription method, like 'subscribeToX' with the actual selection data under it.
      // we need to retrieve this data under root node. 
      var dataScope = topScope.FirstOrDefault().Value; // It can be ObjectScope or plain value
      var msg = new NextMessage() { Id = opId, Type = "next", Payload = dataScope };
      var json = SerializationHelper.Serialize(msg); 
      return json;
    } catch (Exception ex) {
      Trace.WriteLine("Error: " + ex.ToString());
      Debugger.Break();
      return null; 
    }
  }

}
