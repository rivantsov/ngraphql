using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Core.Directives;
using NGraphQL.Core.Introspection;

namespace NGraphQL.CodeFirst {

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : SchemaDirectiveAttribute {
    public readonly string Reason;
    public DeprecatedDirAttribute(string reason) : base(typeof(DeprecatedDirective)) {
      Reason = reason;
    }
  }

  [DirectiveMetaData(
    name: "deprecated",
    description: "Marks element as deprecated.",
    locations: __DirectiveLocation.AllSchemaLocations,
    listInSchema: false
    )]
  public class DeprecatedDirective: Directive {
    public string Reason;

    public DeprecatedDirective(IDirectiveContext context, string reason) : base(context) {
      Reason = reason;
    }
  }



}
