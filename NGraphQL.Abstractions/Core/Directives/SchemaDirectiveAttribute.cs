using System;

namespace NGraphQL.Core {


  /// <summary>Base class for custom attributes identifying custom schema directives; the derived attirbutes 
  /// are to be put on schema elements in GraphQL model definition.  </summary>
  public abstract class SchemaDirectiveAttribute: Attribute {
    public Type DirectiveType;

    public SchemaDirectiveAttribute(Type directiveType) {
      DirectiveType = directiveType; 
    }
  }
}
