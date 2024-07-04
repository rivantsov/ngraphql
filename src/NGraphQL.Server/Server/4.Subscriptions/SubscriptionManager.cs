using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
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

public class ClientConnection {
  public string ConnectionId;
  public ClaimsPrincipal User;
  public string UserId; 
  public ConcurrentDictionary<string, ClientSubscription> Subscriptions = new();
}

public class ClientSubscription {
  public ParsedGraphQLRequest ParsedRequest;
  public string Topic;
}

public class SubscriptionManager {
  IMessageSender _sender;
  GraphQLServer _server;
  // To keep from polluting memory with identical parsed requests, we keep cache  
  ConcurrentDictionary<string, ParsedGraphQLRequest> _cachedParsedRequests = new ();
  ConcurrentDictionary<string, ClientConnection> _connections = new();

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
    _connections[connectionId] = conn; 
  }

  public void OnClientDisconnected(string connectionId, Exception exc) {
    _connections.TryGetValue(connectionId, out var clientConn);
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
    await _sender.Broadcast(null, "Server: " + message);  // this is test code
  }

  // Sequence: SignalRListener -> HandleSubscribeMessage -> ExecuteRequest -> Resolver -> SubscribeCaller(here)
  private async Task HandleSubscribeMessage(ClientConnection client, SubscribeMessage message) {
    var payloadElem = (JsonElement)message.Payload;
    Util.Check(payloadElem.ValueKind == JsonValueKind.Object, "Subscribe.Payload is a JsonElement of invalid type {0}.", message.Type);
    var pload = payloadElem.Deserialize<SubscribePayload>(JsonDefaults.JsonOptions);
    // parse the query
    var rawReq = new GraphQLRequest() { OperationName = pload.OperationName, Query = pload.Query, Variables = pload.Variables };
    var requestContext = new RequestContext(this._server, rawReq, CancellationToken.None);
    requestContext.Subscription = new SubscriptionContext() { Connection = client }; 
    await _server.ExecuteRequestAsync(requestContext);
  }

  // To be called by Subscription Resolver method
  public async Task SubscribeCaller(IFieldContext field, string topic) {
    var reqContext = (RequestContext)field.RequestContext;
    var subCtx = reqContext.GetSubscriptionContext();
    var connId = subCtx.Connection.ConnectionId;
    if (!_connections.TryGetValue(connId, out var clientConn)) {
      return;
    }
    var queryText = reqContext.RawRequest.Query;
    var parsedReq = reqContext.ParsedRequest;
    parsedReq = CacheRequest(queryText, parsedReq);
    var subscr = new ClientSubscription() { Topic = topic, ParsedRequest = parsedReq };
    clientConn.Subscriptions[topic] = subscr;
    await _sender.Subscribe(topic, connId);
  }

  public async Task UnsubscribeCaller(string topic, string connectionId) {
    await _sender.Unsubscribe(topic, connectionId);
    if (!_connections.TryGetValue(connectionId, out var clientConn)) {
      return;
    }
    clientConn.Subscriptions.TryRemove(topic, out var _);
  }



  private ParsedGraphQLRequest CacheRequest(string query, ParsedGraphQLRequest request) {
    if (_cachedParsedRequests.TryGetValue(query, out var cached))
      return cached;
    _cachedParsedRequests.TryAdd(query, request);
    return request; 
  }

  // deserializes message, but leaves Payload as JsonElement, to be later deserialized as a specific subtype
  public static SubscribeMessage DeserializeMessage(string json) {
    var obj = JsonSerializer.Deserialize<SubscribeMessage>(json, JsonDefaults.JsonOptionsSlim); //Slim options is for Payload - do not convert it to dict
    return obj;
  }

}
