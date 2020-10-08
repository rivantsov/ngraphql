using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {
  // hiding these class(es) here in a different namespace, so they do not show up in intellisense 
  // with other attrs in CodeFirst ns

  public abstract class GraphQLTypeRoleAttribute : Attribute {
    internal abstract SchemaTypeRole TypeRole { get; }
  }
}
