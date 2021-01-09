using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

  public class IntrospectionQuery {

    [GraphQLName("__schema"), Hidden]
    public __Schema GetSchema() { return default; }

    [GraphQLName("__type"), Null, Hidden]
    public __Type GetGraphQLType(string name) { return default; }
  }
}