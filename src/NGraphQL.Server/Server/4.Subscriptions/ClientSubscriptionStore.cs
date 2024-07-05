using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Subscriptions;

internal class ClientSubscriptionStore {

  Dictionary<string, ParsedGraphQLRequest> _parsedRequestsCache = new();
  Dictionary<string, SubscriptionVariant> _variantsCache = new();
  Dictionary<string, ClientConnection> _clients = new(); //by connection Id
  Dictionary<string, TopicSubscribers> _topicSubscribers = new(); //by connection Id
  ReaderWriterLockSlim _lock = new();

  public void AddClient(ClientConnection conn) {
    _lock.EnterWriteLock();
    try {
      _clients[conn.ConnectionId] = conn;
    } finally { _lock.ExitWriteLock(); }
  }
 

  public void RemoveClient(string connId) {
    _lock.EnterWriteLock();
    try {
      _clients.SafeRemove(connId);
    } finally { _lock.ExitWriteLock(); }
  }

  public ClientConnection GetClient(string connectionId) {
    _lock.EnterReadLock();
    try {
      return _clients.SafeGet(connectionId);
    } finally { _lock.ExitReadLock(); }
  }

  public ClientSubscription AddSubscription(IRequestContext request, string topic) {
    _lock.EnterWriteLock();
    try {
      var reqContext = (RequestContext)request;
      var subCtx = reqContext.GetSubscriptionContext();
      var connId = subCtx.Connection.ConnectionId;
      if (!_clients.TryGetValue(connId, out var client)) {
        return null;
      }
      var parsedReq = reqContext.ParsedRequest;
      var subscrVariant = GetOrAddVariant(topic, parsedReq);
      var clientSub = AddClientSubscription(client, subscrVariant);
      return clientSub;
    } finally { _lock.ExitWriteLock(); }
  }

  public void RemoveSubscription(string connectionId, string topic) {
    _lock.EnterWriteLock();
    try {
      // remove from the client's list
      if (!_clients.TryGetValue(connectionId, out var clientConn))
        return;
      clientConn.Subscriptions.RemoveAll(cs => cs.Topic == topic);
      // remove from the topic's list
      if (!_topicSubscribers.TryGetValue(topic, out var topicSubs))
        return;
      topicSubs.Subscribers.SafeRemove(connectionId);
      if (topicSubs.Subscribers.Count == 0)
        _topicSubscribers.SafeRemove(topic);
    } finally { _lock.ExitWriteLock(); }
  }

  public IList<ClientSubscription> GetTopicSubscriptions(string topic) {
    _lock.EnterReadLock();
    try {
      if (!_topicSubscribers.TryGetValue(topic, out var topicSubs))
        return _emptyClientSubList;
      var subs = topicSubs.Subscribers.SelectMany(
                           kv => kv.Value.Subscriptions.Where(cs => cs.Topic == topic)
                    ).ToList();
      return subs;
    } finally { _lock.ExitReadLock(); }
  }
  static IList<ClientSubscription> _emptyClientSubList = new ClientSubscription[] { };


// ===================== Private stuff ===============================================
  private ClientSubscription AddClientSubscription(ClientConnection client, SubscriptionVariant subVariant) {
    var clientSub = new ClientSubscription() { Client = client, Variant = subVariant };
    client.Subscriptions.Add(clientSub);
    if (!_topicSubscribers.TryGetValue(subVariant.Topic, out var topicSubs)) {
      topicSubs = new TopicSubscribers() { Topic = subVariant.Topic };
      _topicSubscribers[subVariant.Topic] = topicSubs;
    }
    topicSubs.Subscribers[client.ConnectionId] = client;
    return clientSub;
  }

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
    _parsedRequestsCache[query] = request;
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
