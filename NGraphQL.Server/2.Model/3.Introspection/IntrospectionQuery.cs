using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Introspection {
  [Query]
  public class IntrospectionQuery {

    [GraphQLName("__schema"), Hidden]
    public __Schema GetSchema() { return default; }

    [GraphQLName("__type"), Null, Hidden]
    public __Type GetType(string name) { return default; }
  }
}