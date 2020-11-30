using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.CodeFirst {

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : DirectiveRefAttribute {
    public readonly string Reason;

    public DeprecatedDirAttribute(string reason) : base(typeof(DeprecatedDirective)) {
      Reason = reason;
    }
  }
}
