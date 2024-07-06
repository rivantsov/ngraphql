using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Subscriptions;

namespace NGraphQL.Client {

  public class ClientSubscription {
    public string Id; 
    public string Topic;
    public string Request;
    public Type PayloadType;
    public Action<ClientSubscription, object> OnReceived; 
  }
}
