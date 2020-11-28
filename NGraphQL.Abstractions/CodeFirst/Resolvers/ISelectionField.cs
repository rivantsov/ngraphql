using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Runtime;

namespace NGraphQL.CodeFirst {

  public interface ISelectionField {
    string Name { get; }
    Location Location { get; }
  }
}
