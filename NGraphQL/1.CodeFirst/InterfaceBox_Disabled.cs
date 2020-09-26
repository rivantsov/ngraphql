using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NGraphQL.CodeFirst {

  public abstract class InterfaceBox {

    public readonly object Value;
    public InterfaceBox(object value) {
      Value = value;
    }
  }

  /// <summary>A container class to box the entities returned by a resolver method as instances of API interface. </summary>
  /// <typeparam name="TInt">GraphQL interface type.</typeparam>
  public class InterfaceBox<TInt>: InterfaceBox where TInt: class {
    public InterfaceBox(object value): base(value) { }
  }

  public class InterfaceList<TInt> : List<InterfaceBox<TInt>> where TInt: class { 
    public void AddValue(object value) {
      Add(new InterfaceBox<TInt>(value));
    }
  }
}
