using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  public interface ISelectionField {
    string Name { get; }
    SourceLocation SourceLocation { get; }
  }
}
