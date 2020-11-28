using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Core {

  /// <summary>Arg class is used to define arguments of request directives. </summary>
  /// <typeparam name="T">Argument type.</typeparam>
  public abstract class Arg<T> {
    public abstract T Evaluate(IRequestContext context);
  }
}
