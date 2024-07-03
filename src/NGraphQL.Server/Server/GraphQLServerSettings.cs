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
    IgnoreUnknownJsonFields = 1 << 3,

    DefaultProd = EnableRequestCache | EnableParallelQueries | IgnoreUnknownJsonFields,
    DefaultDev = DefaultProd | ReturnExceptionDetails,

  }

  [Flags]
  public enum GraphQLServerFeatures {
    None = 0, 
    Subscriptions = 1,
    
    RecursiveFragments = 1 << 8,
    
    Default = Subscriptions | RecursiveFragments
  }

  public class GraphQLServerSettings {

    public GraphQLServerOptions Options = GraphQLServerOptions.DefaultDev;
    public GraphQLServerFeatures Features = GraphQLServerFeatures.Default;

    public string SubscriptionsEndpoint = "/subscriptions";

    public int RequestCacheSize = 2000;
    public TimeSpan RequestCacheEvictionTime = TimeSpan.FromSeconds(60);

    /// <summary>Dictionary of custom values for use by extensions, custom sclars or applications. </summary>
    public readonly IDictionary<string, object> Values = new Dictionary<string, object>(); 
  }
  
   
}
