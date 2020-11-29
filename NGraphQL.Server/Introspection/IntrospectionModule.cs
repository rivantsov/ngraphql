using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

  public class IntrospectionModule: GraphQLModule {

    public IntrospectionModule(): base() {
      this.RegisterModelTypes(
        query: typeof(IntrospectionQuery),
        enums: new[] { typeof(TypeKind), typeof(DirectiveLocation) },
        objectTypes: new[] { typeof(__Schema),
        typeof(__Type), typeof(__Field), typeof(__InputValue),
        typeof(__EnumValue), typeof(__Directive)}
      );
      this.RegisterResolvers(typeof(IntrospectionResolvers));
    }

  }
}
