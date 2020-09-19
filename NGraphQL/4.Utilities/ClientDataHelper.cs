using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Utilities {

  public static class ClientDataHelper {

    // Helper methods to retrieve values from Response data tree
    //  
    public static T GetValue<T> (this GraphQLResponse resp, string path) {
      return resp.Data.GetValue<T>(path); 
    }
    
    public static T GetValue<T>(this IDictionary<string, object> data, string path) {
      var keys = path.Split('.', '/');
      object result = data;
      foreach(var key in keys) {
        result = GetByKeyOrIndex(result, key);
      }
      if (result == null)
        return default(T); 
      return (T)result; 
    }

    private static object GetByKeyOrIndex(object data, string key) {
      if (key.StartsWith("#")) {
        if(data == null)
          throw new Exception($"Value is null, cannot lookup value by key '{key}'; expected list.");
        var index = int.Parse(key.Substring(1));
        if(data is IList list)
          return list[index];
        throw new Exception($"Value '{data}' is not a list, cannot lookup value by key '{key}'; expected list.");
      }
      if(data is IDictionary<string, object> dict)
        return dict[key];
      throw new Exception($"Value '{data}' is not a dictionary, cannot lookup value by key '{key}'.");
    }
  }
}
