using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace NGraphQL.Server.Subscriptions;

public class SubscriptionContext {
  public ClientConnection Connection;
  // If IsSubscriptionNextMode is true, we are executing 'subscription-next' action
  // - building data for push notification; We invoke the original 'subscribe' resolver,
  // but set its return Result to be the value provided by 'observer' function.  
  public bool IsSubscriptionNextMode;
  public object SubscriptionNextResolverResult;

}

