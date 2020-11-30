using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst {

  [Directive( name: "@deprecated",
              description: "Marks schema element as deprecated.",
              locations: DirectiveLocation.AllSchemaLocations,
              listInSchema: false )]
  public class DeprecatedDirective : Directive {
    public string Reason;
    public DeprecatedDirective(IDirectiveContext context, string reason) : base(context) {
      Reason = reason;
    }
  }


}
