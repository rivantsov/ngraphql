using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Directives {

  ///<summary>Required interface for directives - attributes or regular classes.</summary> 
  public interface IDirectiveInstance {
    object[] ArgValues { get; } 
  }

}
