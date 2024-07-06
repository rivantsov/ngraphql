using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Subscriptions;

namespace NGraphQL.Client;
using TDict = Dictionary<string, object>;

public class ClientSubscription {
  public string Id; 
  public string Request;
  public TDict Variables; 
  public Type PayloadType;
  public Action<ClientSubscription, object> OnReceived; 
}
