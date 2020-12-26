using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Introspection;

namespace NGraphQL.Core {

  /// <summary>Marks type, field or parameter as deprecated. The element in schema document will appear with @deprecated
  /// directive.</summary>
  [AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface |
    AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : BaseDirectiveAttribute {
    public DeprecatedDirAttribute(string reason) : base(reason) {
    }
  }

}
