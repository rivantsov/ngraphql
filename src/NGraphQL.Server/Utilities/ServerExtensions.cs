using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server.Execution;

namespace NGraphQL.Utilities; 
public static class ServerExtensions {

  public static GraphQLApiModel GetModel(this IRequestContext context) {
    var ctx = (RequestContext)context;
    return ctx.ApiModel; 
  }

  public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) {
    if (dict.TryGetValue(key, out var value))
      return value;
    return default;
  }

  public static void SafeRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) {
    if (dict.ContainsKey(key))
      dict.Remove(key); 
  }
}

