using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text.Json;

namespace NGraphQL.Client {

  public class ServerResponse {
    public readonly ClientRequest Request;

    public HttpStatusCode StatusCode { get; internal set; }

    public string BodyJson { get; internal set; } 
    public IDictionary<string, JsonElement> TopFields { get; internal set; }
    public IList<GraphQLError> Errors { get; internal set; }
    public double DurationMs;
    public Exception Exception;

    public ServerResponse(ClientRequest request) {
      Request = request;
    }


    /* 
      JsonSerializer does not support dynamic objects, so we drop support for it too
    /// <summary>The "data" response field as dynamic object. </summary>
    public dynamic data {
      get { 
        if (_data == null) {
          var dataJObj = this.GetDataJObject();
          _data = dataJObj.ToObject<ExpandoObject>(ClientSerializers.DynamicObjectJsonSerializer);
        }
        return _data; 
      }
    }  object _data;
    */

  }
}

