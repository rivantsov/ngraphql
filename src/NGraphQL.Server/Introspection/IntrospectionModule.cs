using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

  public class IntrospectionModule: GraphQLModule {

    public IntrospectionModule(): base() {
      this.QueryType = typeof(IntrospectionQuery);
      this.EnumTypes.AddRange(new[] { typeof(TypeKind), typeof(DirectiveLocation) });
      this.ObjectTypes.AddRange( new[] { 
        typeof(__Schema), typeof(__Type), typeof(__Field), typeof(__InputValue),
        typeof(__EnumValue), typeof(__Directive)}
      );
      this.ResolverClasses.Add(typeof(IntrospectionResolvers));
    }

  }
}
