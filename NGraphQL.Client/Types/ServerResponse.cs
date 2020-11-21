using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using Newtonsoft.Json.Linq;

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
          _data = DataJObject.ToObject<ExpandoObject>(GraphQLClient.DynamicObjectJsonSerializer);
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

    }

  }

}

