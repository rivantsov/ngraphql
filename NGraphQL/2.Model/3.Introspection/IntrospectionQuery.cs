using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Introspection {
  [Query]
  public class IntrospectionQuery {

    [GraphQLName("__schema")]
    public Schema__ GetSchema() { return default; }

    [GraphQLName("__type"), Null]
    public Type__ GetType(string name) { return default; }
  }
}