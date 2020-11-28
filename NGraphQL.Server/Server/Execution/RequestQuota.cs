using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server.Execution {

  /// <summary>Warning: quota functionality is a draft, was not tested. </summary>
  public class RequestQuota {
    public int MaxDepth = 10;
    public int MaxOutputObjects = 1000;

    /// <summary>Max request time; implemented as timeout of cancellation token. </summary>
    public TimeSpan MaxRequestTime = TimeSpan.FromMinutes(5); 
  }
}
