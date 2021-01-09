using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using Newtonsoft.Json.Linq;
using NGraphQL.Client.Serialization;
using NGraphQL.Client.Utilities;

namespace NGraphQL.Client {

  public class ResponseData {
    public readonly RequestData RequestData;

    public HttpStatusCode StatusCode { get; internal set; }

    public string BodyJson { get; internal set; } 
    public IDictionary<string, JToken> TopFields { get; internal set; }
    public IList<GraphQLError> Errors { get; internal set; }
    public double DurationMs;
    public Exception Exception;

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

    public ResponseData(RequestData request) {
      RequestData = request; 
    }

    public T GetUnmappedFieldValue<T>(object parent, string name) {
      throw new NotImplementedException(); 
    }
  }
}

