using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.Subscriptions;

internal class ClientSubscriptionStore {

  ConcurrentDictionary<string, ParsedGraphQLRequest> _parsedRequestsCache = new();
  ConcurrentDictionary<string, SubscriptionVariant> _variantsCache = new();

  ConcurrentDictionary<string, ClientConnection> _clients = new(); //by connection Id
  ConcurrentDictionary<string, TopicSubscribers> _topicSubscribers = new(); //by connection Id


  // ConcurrentDictionary<string, IList<ClientConnection>> _clientsByTopic = new();


  public void AddClient(ClientConnection conn) {
    _clients[conn.ConnectionId] = conn;
  }

  public void RemoveClient(string connId) {
    _clients.TryRemove(connId, out var _);
  }

  public ClientSubscription AddSubscription(IRequestContext request, string topic) {
    var reqContext = (RequestContext)request;
    var subCtx = reqContext.GetSubscriptionContext();
    var connId = subCtx.Connection.ConnectionId;
    if (!_clients.TryGetValue(connId, out var clientConn)) { 
      return null; 
    }
    var parsedReq = reqContext.ParsedRequest;
    var subscrVariant = GetOrAddVariant(topic, parsedReq);
    var clientSub = AddClientSubscription(clientConn, subscrVariant); 
    return clientSub;
  }

  private ClientSubscription AddClientSubscription(ClientConnection client, SubscriptionVariant subVariant) {
    var clientSub = new ClientSubscription() { Client = client, Variant = subVariant };
    client.Subscriptions.Add(clientSub);
    if (!_topicSubscribers.TryGetValue(subVariant.Topic, out var topicSubs)) {
      topicSubs = new TopicSubscribers() { Topic = subVariant.Topic };
    }
    topicSubs.Subscribers[client.ConnectionId] = client;
    return clientSub; 
  }

  public void RemoveSubscription(string connectionId, string topic) {
    // remove from the client's list
    if (!_clients.TryGetValue(connectionId, out var clientConn))
      return;
    clientConn.Subscriptions.RemoveAll(cs => cs.Topic == topic);
    // remove from the topic's list
    if (!_topicSubscribers.TryGetValue(topic, out var topicSubs))
      return;
    topicSubs.Subscribers.TryRemove(connectionId, out var _);
    if (topicSubs.Subscribers.Count == 0)
      _topicSubscribers.TryRemove(topic, out var _);
  }


  public IList<ClientSubscription> GetTopicSubscriptions(string topic) {
    if (!_topicSubscribers.TryGetValue(topic, out var topicSubs))
      return _emptyClientSubList;
    var subs = topicSubs.Subscribers.SelectMany(
                         kv => kv.Value.Subscriptions.Where(cs => cs.Topic == topic)
                  ).ToList();
    return subs;
  }
  static IList<ClientSubscription> _emptyClientSubList = new ClientSubscription[] { };

  private SubscriptionVariant GetOrAddVariant(string topic, ParsedGraphQLRequest request) {
    request = GetOrAddCacheParsedRequest(request);
    var key = SubscriptionVariant.GetLookupKey(topic, request.Query);
    if (_variantsCache.TryGetValue(key, out var subVariant))
      return subVariant;
    // protection against flood attack
    CheckPurgeDictionary(_variantsCache, 1000);
    subVariant = new SubscriptionVariant(topic, request, key);
    _variantsCache[key] = subVariant;
    return subVariant;
  }

  private ParsedGraphQLRequest GetOrAddCacheParsedRequest(ParsedGraphQLRequest request) {
    var query = request.Query;
    if (_parsedRequestsCache.TryGetValue(query, out var cached))
      return cached;
    // protection against flood attack
    CheckPurgeDictionary(_parsedRequestsCache, 1000);
    _parsedRequestsCache.TryAdd(query, request);
    return request;
  }

  private static void CheckPurgeDictionary<T>(IDictionary<string, T> dict, int max) {
    while (dict.Count <= max)
      return;
    var newCount = max * 9 / 10; // drop 10%
    while(dict.Count > newCount)
      dict.Remove(dict.First().Key);
  }


}
