using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Subscriptions; 

public interface IMessageSender {
  Task SendMessage(string connectionId, string message);
}

public class ClientConnection {
  public string ConnectionId;
  public ClaimsPrincipal User;
  public string UserId;
  public List<ClientSubscriptionInfo> Subscriptions = new();
}

public class SubscriptionVariant {
  public string Topic;      //  Ex: ThingUpdate/123
  public ParsedGraphQLRequest ParsedRequest;
  public string LookupKey;  // Topic/QueryText

  public SubscriptionVariant(string topic, ParsedGraphQLRequest parsedRequest, string lookupKey) {
    Topic = topic;
    ParsedRequest = parsedRequest;
    LookupKey = lookupKey;
  }

  public static string MakeLookupKey(string topic, string queryText) {
    return $"{topic}/{queryText}";
  }
  public override string ToString() => Topic; 

}

public class ClientSubscriptionInfo {
  public string Id;
  public ClientConnection Client;
  public SubscriptionVariant Variant;
  public string Topic => Variant.Topic;

  public override string ToString() => $"{Topic}/{Client.ConnectionId}";
}

public class TopicSubscribers {
  public string Topic;
  public Dictionary<string, ClientConnection> Subscribers = new(); 
}

// Sits on RequestContext
public class SubscriptionContext {
  public ClientConnection Client;
  public string ClientSubscriptionId;
}


