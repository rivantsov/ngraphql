using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server {

  // ExecutionExtensions implements IsSet method for this enum
  [Flags]
  public enum GraphQLServerOptions {
    None = 0,
    ReturnExceptionDetails = 1,
    EnableRequestCache = 1 << 1,
    EnableParallelQueries = 1 << 2,

    DefaultProd = EnableRequestCache | EnableParallelQueries,
    DefaultDev = DefaultProd | ReturnExceptionDetails,

  }

  public class GraphQLServerSettings {
    public GraphQLServerOptions Options = GraphQLServerOptions.DefaultDev;

    public int RequestCacheSize = 2000;
    public TimeSpan RequestCacheEvictionTime = TimeSpan.FromSeconds(60);

    /// <summary>Dictionary of custom values for use by extensions, custom sclars or applications. </summary>
    public readonly IDictionary<string, object> Values = new Dictionary<string, object>(); 
  }
  
   
}
