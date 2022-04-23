using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Core.Scalars {

  public abstract class CustomScalar: Scalar {

    public CustomScalar(string name, string description, Type defaultClrType) : base(name, description, defaultClrType, isCustom: true) { 
    }

    }//class

  }
