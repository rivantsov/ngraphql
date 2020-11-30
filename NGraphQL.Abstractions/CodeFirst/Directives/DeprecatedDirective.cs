using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst {

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : SchemaDirectiveAttribute {
    public readonly string Reason;
    public DeprecatedDirAttribute(string reason)  {
      Reason = reason;
    }

    [ImplementsDirective(
      name: "@deprecated",
      description: "Marks element as deprecated.",
      locations: DirectiveLocation.AllSchemaLocations,
      listInSchema: false
      )]
    public DeprecatedDirectiveResult Invoke(IDirectiveContext context, string reason) {
      return new DeprecatedDirectiveResult(context, reason); 
    }
  }

  public class DeprecatedDirectiveResult: DirectiveAction {
    public string Reason;


    public DeprecatedDirectiveResult(IDirectiveContext context, string reason) : base(context.MetaData) {
      Reason = reason;
    }
  }



}
