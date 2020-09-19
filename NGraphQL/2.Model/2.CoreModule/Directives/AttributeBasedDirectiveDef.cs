using NGraphQL.CodeFirst;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model.Core {

  public abstract class AttributeBasedDirectiveDef : DirectiveDef {
    public abstract Directive CreateDirective(GraphQLApiModel model, DirectiveBaseAttribute attr, object ownerInfo);
  }

}
