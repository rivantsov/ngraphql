using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Subscriptions {
  // based on protocol patterns from here: https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md
  
  public class MessageBase {
    public string Id;
    public string Type;
    public object Payload;
  }

  public class SubscribePayload {
    public string OperationName;
    public string Type;
    public Dictionary<string, object> Variables;
    public Dictionary<string, object> Extensions;
  }

}
