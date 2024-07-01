using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace Things.GraphQL.Types {

  public interface IThingsSubscription {

    bool Subscribe(string childName);
  }
}
