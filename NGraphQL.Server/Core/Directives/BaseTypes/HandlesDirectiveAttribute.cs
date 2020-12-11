using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core;

namespace NGraphQL.Model {

  /// <summary>Identifies the directive for a directive handler class. </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class HandlesDirectiveAttribute : Attribute {
    public readonly string DirectiveName; 

    public HandlesDirectiveAttribute(string directiveName) {
      DirectiveName = directiveName;
    }
  }

}
