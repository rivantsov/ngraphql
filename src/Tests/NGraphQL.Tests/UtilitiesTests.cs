using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;

namespace NGraphQL.Tests {

  [TestClass]
  public class UtilitiesTests {

    [TestCleanup]
    public void Cleanup() {
      AppTime.ClearOffset(); //make sure there's no time shift when test is completed
    }

    [TestMethod]
    public void Test_Utils_DoubleBufferCache() {
      TestEnv.LogTestMethodStart();
      var cache = new DoubleBufferCache<string, string>(capacity: 20, evictionTimeSec:  10);
      // fill out 10 items
      for (int i = 0; i < 10; i++)
        cache.Add($"Key-{i}", $"Value-{i}");
      // read 20 items, 10 must be in cache
      var hits = 0;
      var misses = 0; 
      for(int i = 0; i < 20; i++) {
        if (cache.TryLookup($"Key-{i}", out _))
          hits++;
        else
          misses++;
      }
      // check that we have 10 hits and 10 misses
      Assert.AreEqual(10, hits, "Invalid hit count");
      Assert.AreEqual(10, misses, "Invalid miss count");
      // move time forward, and add item that is already there. this will force swapping buffers
      // and finalizing metrics record
      AppTime.SetOffset(TimeSpan.FromSeconds(30));
      cache.Add("Key-0", "Value-0");
      // check metrics
      var metrics = cache.GetMetrics();
      Assert.AreEqual(10, metrics.ItemCount);
      Assert.AreEqual(20, metrics.ReadCount);
      Assert.AreEqual(10, metrics.MissCount);

      AppTime.ClearOffset(); 
    }
  }
}
