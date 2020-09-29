using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  [Subscription]
  public interface IThingsSubscription {

    bool Subscribe(string childName);
  }
}
