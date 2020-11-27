using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Introspection {

  public class IntrospectionModule: GraphQLModule {

    public IntrospectionModule(GraphQLApi api): base(api) {
      this.RegisterTypes(
        typeof(IntrospectionQuery),
        typeof(TypeKind), typeof(DirectiveLocation),
        typeof(__Schema), typeof(__Type), typeof(__Field), typeof(__InputValue),
        typeof(__EnumValue), typeof(__Directive)
      );
      this.RegisterResolvers(typeof(IntrospectionResolvers));
    }

  }
}
