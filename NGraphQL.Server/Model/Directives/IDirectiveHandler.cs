using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {
  public interface IDirectiveHandler {
    T GetInterface<T>()
  }
}
