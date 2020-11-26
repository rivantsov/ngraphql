using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model.Core {

  public class DeprecatedDirDef: AttributeBasedDirectiveDef {
    private InputValueDef _reasonArgDef;

    public DeprecatedDirDef(CoreModule sys) {
      base.Name = "@deprecated";
      base.Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.Enum | DirectiveLocation.EnumValue |
         DirectiveLocation.FieldDefinition | DirectiveLocation.InputFieldDefinition | DirectiveLocation.InputObject |
         DirectiveLocation.Interface | DirectiveLocation.Mutation | DirectiveLocation.Object | DirectiveLocation.Scalar
      ;
      _reasonArgDef = new InputValueDef() { Name = "reason", TypeRef = sys.String_.TypeRefNotNull };
      base.Args = new InputValueDef[] { _reasonArgDef };
    }

    public override Directive CreateDirective(GraphQLApiModel model, DirectiveBaseAttribute attr, object ownerInfo) {
      var deprAttr = (DeprecatedDirAttribute)attr;
      return this.CreateDirective(deprAttr.Reason);
    }

  }
}
