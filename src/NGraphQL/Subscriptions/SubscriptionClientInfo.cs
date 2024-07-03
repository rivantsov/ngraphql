using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace NGraphQL.Subscriptions {

  public class SubscriptionClientInfo {
    public ClaimsPrincipal User;
    public string UserIdentifier;
    public string ConnectionId;
    public object Context; //SignalR HubCallerContext
  }

}
