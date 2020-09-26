using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Introspection {

  public class IntrospectionModule: GraphQLModule {

    public IntrospectionModule() {
      this.RegisterTypes(
        typeof(IntrospectionQuery),
        typeof(TypeKind), typeof(DirectiveLocation),
        typeof(Schema__), typeof(Type__), typeof(Field__), typeof(InputValue__),
        typeof(EnumValue__), typeof(Directive__)
      );
      this.RegisterResolversClass<IntrospectionResolvers>();
    }

  }
}
