using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  public interface IIncludeSkipDirective {
    bool IsIncluded(IRequestContext context);
  }

}
