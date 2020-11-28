using System;
using System.Threading;

using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public class RequestCache {

    public class RequestCacheItem {
      public string Key;
      public ParsedGraphQLRequest ParsedRequest;
      public int UseCount;
      public DateTime CreatedOn; 
    }

    DoubleBufferCache<string, RequestCacheItem> _cache;
    public bool Enabled;

    public RequestCache(GraphQLServerSettings settings) {
      _cache = new DoubleBufferCache<string, RequestCacheItem>(settings.RequestCacheSize, 
                                      settings.RequestCacheEvictionTime.TotalSeconds);
      Enabled = settings.Options.IsSet(GraphQLServerOptions.EnableRequestCache);
    }

    public bool TryLookupParsedRequest(RequestContext context) {
      if (!Enabled)
        return false;
      var query = context.RawRequest.Query;
      if (_cache.TryLookup(query, out var item)) {
        Interlocked.Increment(ref item.UseCount);
        context.ParsedRequest = item.ParsedRequest;
        context.Metrics.FromCache = true;
        return true; 
      }
      return false; 
    }

    public void AddParsedRequest(RequestContext context) {
      if (!Enabled)
        return;
      if (context.Metrics.FromCache)
        return; //already cached
      var reqText = context.RawRequest.Query;
      var item = new RequestCacheItem() { CreatedOn = AppTime.UtcNow, Key = reqText, ParsedRequest = context.ParsedRequest};
      _cache.Add(reqText, item); 
    }

    public CacheMetrics GetCurrentMetrics() {
      return _cache.GetMetrics(); 
    }
  }
}
