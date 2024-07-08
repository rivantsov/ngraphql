using System;
using System.Collections.Generic;
using System.Text;

// based on protocol patterns from here: https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md

namespace NGraphQL.Subscriptions;
using TDict = Dictionary<string, object>;

public class SubscriptionMessage {
  public string Type;
}

public class PayloadMessage<TPayload> : SubscriptionMessage {
  public string Id;
  public TPayload Payload;
}
public class PayloadMessage : PayloadMessage<object> { }

public class SubscribeMessage: PayloadMessage<SubscribePayload> {
  public SubscribeMessage(string id, SubscribePayload payload) {
    Type = SubscriptionMessageTypes.Subscribe;
    Id = id;
    Payload = payload;
  }
}

public class SubscribePayload {
  public string OperationName;
  public string Query;
  public Dictionary<string, object> Variables;
  public Dictionary<string, object> Extensions;
}


public class NextMessage<T>: PayloadMessage<T> { 
  public NextMessage() { Type = SubscriptionMessageTypes.Next; }
}
public class NextMessage : NextMessage<object> { }

public class CompleteMessage : SubscriptionMessage {
  public string Id;
  public CompleteMessage() { Type = SubscriptionMessageTypes.Complete; }
}

public class ErrorMessage : PayloadMessage<GraphQLError[]> {
  public ErrorMessage() {Type = SubscriptionMessageTypes.Error; }
}

public abstract class PayloadDictMessageBase : SubscriptionMessage {
  public TDict Payload;
}

public class ConnectionInitMessage : PayloadDictMessageBase {
  public ConnectionInitMessage() { Type = SubscriptionMessageTypes.ConnectionInit; }
}
public class ConnectionAckMessage : PayloadDictMessageBase {
  public ConnectionAckMessage() { Type = SubscriptionMessageTypes.ConnectionAck; }
}

public class PingMessage : PayloadDictMessageBase {
  public PingMessage() { Type = SubscriptionMessageTypes.Ping; }
}
public class PongMessage : PayloadDictMessageBase {
  public PongMessage() { Type = SubscriptionMessageTypes.Pong; }
}


public static class SubscriptionMessageTypes {
  public const string ConnectionInit = "connection_init";
  public const string ConnectionAck = "connection_ack";
  public const string Subscribe = "subscribe";
  public const string Next = "next";
  public const string Error = "error";
  public const string Complete = "complete";
  public const string Ping = "ping";
  public const string Pong = "pong";

}
