using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  // Query, Mutation and Subscription types can be either class or interface; we use interface here
  [Subscription]
  public interface IThingsSubscription {

    bool Subscribe(string childName);
  }
}
