using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Subscriptions {
  // based on protocol patterns from here: https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md
  public class SubscriptionMessage {
    public string Id;
    public string Type;
  }

  public class PayloadMessage<TPayload> : SubscriptionMessage {
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

  public class NextMessage<T>: PayloadMessage<T> { 
    public NextMessage() { Type = "next"; }
  }
  public class NextMessage : NextMessage<object> { }

  public class CompleteMessage : SubscriptionMessage {
    public CompleteMessage() { Type = "complete"; }
  }

  public class ErrorMessage : PayloadMessage<GraphQLError[]> {
    public ErrorMessage() {Type = "error"; }
  }

  public class SubscribePayload {
    public string OperationName;
    public string Query;
    public Dictionary<string, object> Variables;
    public Dictionary<string, object> Extensions;
  }

  public static class SubscriptionMessageTypes {
    public const string Subscribe = nameof(Subscribe);
    public const string Next = nameof(Next);
    public const string Error = nameof(Error);
    public const string Complete = nameof(Complete);
    public const string Invalid = nameof(Invalid);

  }
}
