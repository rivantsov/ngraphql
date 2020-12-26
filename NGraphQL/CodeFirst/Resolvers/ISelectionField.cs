using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Server;

namespace NGraphQL.CodeFirst {

  public interface ISelectionField {
    string Name { get; }
    QueryLocation Location { get; }
  }
}
