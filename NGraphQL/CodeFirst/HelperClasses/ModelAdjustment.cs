using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  public enum AdjustmentType {
    IgnoreMember,
    GraphQLName,
  }

  public class ModelAdjustment {
    public AdjustmentType Type;
    public object Target;
  }
}
