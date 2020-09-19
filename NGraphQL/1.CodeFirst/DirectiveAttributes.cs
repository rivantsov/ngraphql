using System;

using NGraphQL.Model.Core;

namespace NGraphQL.CodeFirst {

  public abstract class DirectiveBaseAttribute : Attribute {
    public Type DirectiveDefType;

    public DirectiveBaseAttribute(Type directiveDefType) {
      DirectiveDefType = directiveDefType; 
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class DeprecatedDirAttribute : DirectiveBaseAttribute {
    public readonly string Reason;
    public DeprecatedDirAttribute(string reason) : base(typeof(DeprecatedDirDef)) {
      Reason = reason;
    }
  }

  /*
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property|AttributeTargets.Method|AttributeTargets.Parameter)]
  public class MaxLenAttribute : DirectiveBaseAttribute {
    public int Len;
    public MaxLenAttribute(int len): base("@maxlen") {
      Len = len;
    }
  }

  public class FormatAttribute : DirectiveBaseAttribute {
    public string Format;
    public FormatAttribute(string format): base("@format") {
      this.Format = format;
    }
  }
  */
}
