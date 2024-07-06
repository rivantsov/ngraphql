using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Subscriptions; 

public interface IMessageSender {
  Task Publish(string message, IList<string> connectionIds);
}

public class ClientConnection {
  public string ConnectionId;
  public ClaimsPrincipal User;
  public string UserId;
  public List<ClientSubscription> Subscriptions = new();
}

public class SubscriptionVariant {
  public string Id;    
  public string Topic;      //  Ex: ThingUpdate/123
  public ParsedGraphQLRequest ParsedRequest;
  public string LookupKey;  // Topic/QueryText

  public SubscriptionVariant(string topic, ParsedGraphQLRequest parsedRequest, string lookupKey = null) {
    Topic = topic;
    ParsedRequest = parsedRequest;
    LookupKey = lookupKey ?? GetLookupKey(topic, parsedRequest.Query);
    var variantId = Interlocked.Increment(ref _variantCount);
    Id = $"{topic}/{variantId}";
  }

  public static string GetLookupKey(string topic, string queryText) {
    return $"{topic}/{queryText}";
  }

  private static long _variantCount;
}

// We need collections by: Client, Topic
public class ClientSubscription {
  public ClientConnection Client;
  public SubscriptionVariant Variant;
  public string Topic => Variant.Topic;
}

public class TopicSubscribers {
  public string Topic;
  public Dictionary<string, ClientConnection> Subscribers = new(); 
}


