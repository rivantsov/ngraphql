using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst {

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  [DefineDirective(name: "@deprecated", description: "Marks schema element as deprecated.",
                locations: DirectiveLocation.AllSchemaLocations, listInSchema: false)]
  public class DeprecatedDirAttribute : SchemaDirectiveAttribute {
    public readonly string Reason;

    public DeprecatedDirAttribute(string reason) : base(reason) {
      Reason = reason;
    }
  }

}
