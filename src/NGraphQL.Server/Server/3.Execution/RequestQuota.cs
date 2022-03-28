using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server.Execution {

  /// <summary>Warning: quota functionality is a draft, was not tested. </summary>
  public class RequestQuota {
    public int MaxDepth = 100;
    public int MaxOutputObjects = 10 * 1000; //reasonable default large limit

    /// <summary>Max request time; implemented as timeout of cancellation token. </summary>
    public TimeSpan MaxRequestTime = TimeSpan.FromMinutes(5);

    public const int MaxErrors = 100;
  }
}
