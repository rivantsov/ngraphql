using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using Newtonsoft.Json.Linq;
using NGraphQL.Client.Serialization;

namespace NGraphQL.Client {

  public class ServerResponse {
    public readonly ClientRequest Request;
    public HttpStatusCode Status { get; internal set; }

    public string BodyJson { get; internal set; } 
    public IDictionary<string, JToken> Body { get; internal set; }
    public IList<RequestError> Errors { get; internal set; }
    public JObject DataJObject { get; internal set; }
    public int TimeMs;
    public Exception Exception;

    /// <summary>The "data" response field as dynamic object. </summary>
    public dynamic data {
      get {
        if (_data == null)
          _data = DataJObject.ToObject<ExpandoObject>(ClientSerializers.DynamicObjectJsonSerializer);
        return _data; 
      }
    } 

    GraphQLClient _client;
    object _data;

    public ServerResponse(GraphQLClient client, ClientRequest request) {
      _client = client;
      Request = request; 
    }

    public T GetField<T>(string name) {
      if (this.DataJObject == null)
        throw new Exception("'data' element was not returned by the request. See errors in response.");
      if (!this.DataJObject.TryGetValue(name, out var jtoken))
        throw new Exception($"Field '{name}' not found in response.");
      var type = typeof(T);
      var nullable = ClientExtensions.CheckNullable(ref type);
      if (jtoken == null) {
        if (nullable)
          return (T) (object) null;
        throw new Exception($"Field '{name}': cannot convert null value to type {typeof(T)}.");
      }
      if (jtoken is JValue jv && !type.IsValueType)
        return (T) jv.Value;
      // deserialize as type
      var res = jtoken.ToObject<T>(ClientSerializers.TypedJsonSerializer);
      return res; 
    }

  }
}

