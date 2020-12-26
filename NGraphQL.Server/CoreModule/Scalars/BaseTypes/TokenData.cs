using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server;

namespace NGraphQL.Core.Scalars {

  public class TokenData {
    public string TermName;
    public string Text;
    public object ParsedValue;
    public QueryLocation Location; 
  }

}
