using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server {

  /// <summary>Request processing metrics data.</summary>
  public class RequestMetrics {
    /// <summary>Request processing start.</summary>
    public DateTime StartedOn;

    /// <summary>Request processing duration.</summary>
    public TimeSpan Duration;

    /// <summary>Number of threads used to execute top operation fields in parallel.</summary>
    public int ExecutionThreadCount;

    /// <summary>Request processing time by HttpServer if any.</summary>
    public TimeSpan HttpRequestDuration;

    /// <summary>Count of the resolver method calls.</summary>
    public int ResolverCallCount;

    /// <summary>Indicates that the request was not parsed from source text but parsed instance was retrieved from cache. </summary>
    public bool FromCache;

    /// <summary>Count of the output objects.</summary>
    public int OutputObjectCount;

  }
}
