using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NGraphQL.Utilities {

  public class CacheMetrics {
    public DateTime StartedOn;
    public DateTime EndedOn; 
    public int ItemCount; 
    public int ReadCount;
    public int MissCount; 
  }

  public class DoubleBufferCache<TKey, TValue> where TValue: class {

    IDictionary<TKey, TValue> _recentItems;
    IDictionary<TKey, TValue> _olderItems;

    int _capacity;
    DateTime _lastSwappedOn;
    TimeSpan _swapInterval;
    DateTime _nextSwapOn; 
    // when we swap buffers, we save current metrics in _oldMetrics
    CacheMetrics _metrics;
    CacheMetrics _oldMetrics;
    object _lock = new object(); 

    public DoubleBufferCache(int capacity = 2000, double evictionTimeSec = 20) {
      _capacity = capacity;
      // we set swap interval to half evict time, as stale items stay in cache for time which 
      // is double of swap interval - first item sits in recent items, then in older items
      _swapInterval = TimeSpan.FromSeconds(evictionTimeSec / 2);
      _recentItems = new Dictionary<TKey, TValue>(capacity + 10);
      _olderItems = new Dictionary<TKey, TValue>(capacity + 10);
      // create initial metrics and make a buffer swap to finish full initialization
      _metrics = new CacheMetrics() { StartedOn = AppTime.UtcNow };
      SwapBuffers(); 
    }

    public bool TryLookup(TKey key, out TValue value) {
      lock(_lock) {
        try {
          _metrics.ReadCount++; 
          if (_recentItems.TryGetValue(key, out value))
            return true;
          if (_olderItems.TryGetValue(key, out value)) {
            _recentItems[key] = value;
            return true;
          }
          _metrics.MissCount++;
          return false;
        } finally {
          // do not check every time, only once in 10 reads
          if (_metrics.ReadCount % 10 == 0)
            CheckNeedSwapBuffers();
        }
      }
    }

    public void Add(TKey key, TValue value) {
      lock(_lock) {
        _recentItems[key] = value;
        CheckNeedSwapBuffers(); 
      }
    }

    public void Clear() {
      lock(_lock) {
        _recentItems.Clear();
        _olderItems.Clear(); 
      }
    }

    public CacheMetrics GetMetrics() => _oldMetrics; 

    private void CheckNeedSwapBuffers() {
      var needSwap = _recentItems.Count > _capacity || AppTime.UtcNow > _nextSwapOn;
      if (needSwap)
        SwapBuffers();
    }

    private void SwapBuffers() {
      var utcNow = AppTime.UtcNow;
      //swap metrics
      _metrics.ItemCount = Math.Max(_recentItems.Count, _olderItems.Count);
      _metrics.EndedOn = utcNow;
      _oldMetrics = _metrics;
      _metrics = new CacheMetrics() { StartedOn = utcNow };
      //swap buffers
      var temp = _olderItems;
      _olderItems = _recentItems;
      _recentItems = temp; 
      _recentItems.Clear();
      // update times
      _lastSwappedOn = utcNow;
      _nextSwapOn = utcNow.Add(_swapInterval);
    }

  }

}
