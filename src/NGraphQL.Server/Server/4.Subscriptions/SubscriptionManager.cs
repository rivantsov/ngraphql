﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Irony.Parsing;
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
  public void Init(IMessageSender sender) {
    _sender = sender; 
  }

  public void OnClientConnected(string connectionId, ClaimsPrincipal user, string userId) {
    var conn = new ClientConnection() { ConnectionId = connectionId, User = user, UserId = userId };
    _subscriptionStore.AddClient(conn); 
  }

  public void OnClientDisconnected(string connectionId, Exception exc) {
    _subscriptionStore.RemoveClient(connectionId);  
  }
     
  public async Task MessageReceived(ClientConnection client, string message) {
    try {
      Util.Check(_sender != null, "Subscription manager not initialized, message sender not set.");
      await MessageReceivedImpl(client, message); 
    } catch(Exception ex) {
      Trace.WriteLine($"MessageReceived error: {ex.ToString()}");
      Debugger.Break(); 
    }
  }
  
  private async Task MessageReceivedImpl(ClientConnection client, string message) {
    var msg = DeserializeMessage(message); 
    switch(msg.Type) {
      case SubscriptionMessageTypes.Subscribe:
        await HandleSubscribeMessage(client, msg); 
        break;
      case SubscriptionMessageTypes.Complete:
        break;
      default:
        break;
    }
    await _sender.Publish("Server: " + message, new string[] { client.ConnectionId });  // this is test code
  }

  // Sequence: SignalRListener -> HandleSubscribeMessage -> ExecuteRequest -> Resolver -> AddSubscription(here)
  private async Task HandleSubscribeMessage(ClientConnection client, SubscribeMessage message) {
    var payloadElem = (JsonElement)message.Payload;
    Util.Check(payloadElem.ValueKind == JsonValueKind.Object, "Subscribe.Payload is a JsonElement of invalid type {0}.", message.Type);
    var pload = payloadElem.Deserialize<SubscribePayload>(JsonDefaults.JsonOptions);
    // parse the query
    var rawReq = new GraphQLRequest() { OperationName = pload.OperationName, Query = pload.Query, Variables = pload.Variables };
    var requestContext = new RequestContext(this._server, rawReq, CancellationToken.None);
    requestContext.Subscription = new SubscriptionContext() { Connection = client }; 
    await _server.ExecuteRequestAsync(requestContext); //in the call, the resolver adds subscription using AddSubscription method below
  }

  // To be called by Subscription Resolver method
  public async Task SubscribeCaller(IFieldContext field, string topic) {
    var clientSub = _subscriptionStore.AddSubscription(field.RequestContext, topic);
    await Task.CompletedTask;
  }

  public async Task UnsubscribeCaller(string topic, string connectionId) {
    _subscriptionStore.RemoveSubscription(topic, connectionId);
    await Task.CompletedTask;
  }

  public async Task Publish(string topic, object payload) {
    var task = Task.Run(async () => await PublishImpl(topic, payload));
    await Task.CompletedTask;
  }

  private async Task PublishImpl(string topic, object payload) {
    var subs = _subscriptionStore.GetTopicSubscriptions(topic);
    // Group by sub variant
    var varGroups = subs.GroupBy(sub => sub.Variant);
    // Each group contains Subscriptions with the same Variant (topic and query), so all clients will get identical message
    foreach(var grp in varGroups) {
      var subscrVariant = grp.Key;
      var groupSubs = grp.ToList();
      var connIds = groupSubs.Select(cs => cs.Client.ConnectionId).ToList();
      var msgJson = await BuildMessage(subscrVariant, payload);
      await _sender.Publish(msgJson, connIds);
    }
  }

  private async Task<string> BuildMessage(SubscriptionVariant sub, object data) {
    var opId = $"{sub.Topic}/{Guid.NewGuid()}";
    var subContext = new SubscriptionContext() { IsSubscriptionNextMode = true, SubscriptionNextResolverResult = data };
    var reqContext = new RequestContext(_server, sub.ParsedRequest, subContext);
    var reqHandler = new RequestHandler(_server, reqContext);
    var topOp = sub.ParsedRequest.Operations.First();
    var topScope = (OutputObjectScope) reqContext.Response.Data;
    await reqHandler.ExecuteOperationAsync(topOp, topScope);
    var msg = new NextMessage() { Id = opId, Type = "next", Payload = reqContext.Response.Data };
    var json = SerializationHelper.Serialize(msg);
    return json;    
  }

  // deserializes message, but leaves Payload as JsonElement, to be later deserialized as a specific subtype
  public static SubscribeMessage DeserializeMessage(string json) {
    //Slim options is for message.Payload - do not convert it to dict, leave as JsonElement
    var obj = JsonSerializer.Deserialize<SubscribeMessage>(json, JsonDefaults.JsonOptionsSlim); 
    return obj;
  }

}
