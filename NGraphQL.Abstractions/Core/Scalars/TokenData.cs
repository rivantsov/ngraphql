using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Runtime;

namespace NGraphQL.Core {

  public class TokenData {
    public string TermName;
    public string Text;
    public object ParsedValue;
    public Location Location; 
  }

}
