using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Subscriptions {
  // based on protocol patterns from here: https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md
  
  public class SubscribeMessage {
    public string Id;
    public string Type;
    public object Payload;
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
