using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Client {

  /// <summary> Base class for client data objects - types that represent your data model. Defines __typename field
  /// that allows the derived type to support the standard introspection field __typename. 
  /// </summary>
  /// <remarks>You do not have to derive your client side data classes from this type, it is provided for convenience.</remarks>
  public class ClientDataType {
    public string __typename; 
  }
}
