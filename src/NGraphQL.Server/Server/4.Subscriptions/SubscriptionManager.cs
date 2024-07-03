using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NGraphQL.Json;
using NGraphQL.Server.Execution;
using NGraphQL.Subscriptions;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Subscriptions; 

public class SubscriptionManager {
  IMessageSender _sender;
  GraphQLServer _server; 

  public SubscriptionManager(GraphQLServer server) {
    //_sender = sender;
    _server = server;
  }

  // provides a way for SignalRSender singleton to register with SubscriptionManager. 
  // It's a bit complicated, SignalRListener (hub, transient) and SignalRSender are created by DI, late, when the first message is sent.
  // It starts from Listener, it has Sender as parameter(not used) that forces DI to create the instance (singleton),
  // and sender immediately registers itself with SubscriptionManager. 
  public void Init(IMessageSender sender) {
    _sender = sender; 
  }

  public async Task Subscribe(string topic, string subscriber) {
    await _sender.Subscribe(topic, subscriber);
  }

  public async Task Unsubscribe(string topic, string subscriber) {
    await _sender.Unsubscribe(topic, subscriber);
  }


  public async Task MessageReceived(SubscriptionClientInfo client, string message) {
    try {
      Util.Check(_sender != null, "Subscription manager not initialized, message sender not set.");
      await MessageReceivedImpl(client, message); 
    } catch(Exception ex) {
      Trace.WriteLine($"MessageReceived error: {ex.ToString()}");
      Debugger.Break(); 
    }

  }
  
  private async Task MessageReceivedImpl(SubscriptionClientInfo client, string message) {
    var msg = DeserializeMessage(message); 
    switch(msg.Type) {
      case SubscriptionMessageTypes.Subscribe:
        await HandleSubscribe(client, msg); 
        break;
      case SubscriptionMessageTypes.Complete:
        break;
      default:
        break;
    }
    await _sender.Broadcast(null, "Server: " + message);  // this is test code
  }


  private async Task HandleSubscribe(SubscriptionClientInfo client, SubscriptionMessage message) {
    var payloadElem = (JsonElement)message.Payload;
    Util.Check(payloadElem.ValueKind == JsonValueKind.Object, "Subscribe.Payload is a JsonElement of invalid type {0}.", message.Type);
    var pload = payloadElem.Deserialize<SubscribePayload>(JsonDefaults.JsonOptions);
    // parse the query
    var rawReq = new GraphQLRequest() { OperationName = pload.OperationName, Query = pload.Query, Variables = pload.Variables };
    var requestContext = new RequestContext(this._server, rawReq, CancellationToken.None, client.User);
    requestContext.SubscriptionClient = client; 
    await _server.ExecuteRequestAsync(requestContext);
  }

  // deserializes message, but leaves Payload as JsonElement, to be later deserialized as a specific subtype
  public static SubscriptionMessage DeserializeMessage(string json) {
    var obj = JsonSerializer.Deserialize<SubscriptionMessage>(json, JsonDefaults.JsonOptionsSlim); //Slim options is for Payload - do not convert it to dict
    return obj;
  }

}
