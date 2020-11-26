using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {

  public class UnionBase {
    public object Value; 
  }

  public class Union<T0, T1> : UnionBase { }
  
  public class Union<T0, T1, T2> : UnionBase { }
  
  public class Union<T0, T1, T2, T3> : UnionBase { }
  
  public class Union<T0, T1, T2, T3, T4> : UnionBase { }
  
  public class Union<T0, T1, T2, T3, T4, T5> : UnionBase { }
  
  public class Union<T0, T1, T2, T3, T4, T5, T6> : UnionBase { }
  
  public class Union<T0, T1, T2, T3, T4, T5, T6, T7> : UnionBase { }

}
