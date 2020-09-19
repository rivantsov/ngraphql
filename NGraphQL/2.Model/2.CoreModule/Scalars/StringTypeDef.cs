using System;
using System.Collections.Generic;
using System.Text;

using Irony.Parsing;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model.Core {

  public class StringTypeDef : ScalarTypeDef {

    public StringTypeDef(string name = "String", bool isCustom = false) : base(name, typeof(string), isCustom) { }

    // The string is actually already parsed by Irony parser (stripped quotes, unescaped, etc),
    //  so here we just return the same value
    public override object ParseToken(RequestContext context, TokenValueSource tokenInput) {
      var tkn = tokenInput.TokenData;
      switch(tkn.TermName) {
        case TermNames.NullValue:
          return null;
        case TermNames.StrSimple:
        case TermNames.StrBlock:
          return tkn.ParsedValue;
      }
      context.ThrowScalarInput($"Invalid text value: '{tkn.Text}'", tokenInput);
      return null;
    }

    const char _dquote = '"';
    const char _backSlash = '\\';
    static char[] _charsToEscape = new char[] { _dquote, _backSlash };

    public override string FormatConstant(object value) {
      if(value == null)
        return "null";
      var strValue = (value is string str) ? str : value.ToString();
      if(strValue.IndexOfAny(_charsToEscape) >= 0)
        strValue = strValue.Replace("\\", "\\\\").Replace("\"", "\\\"");
      return _dquote + strValue + _dquote; 
    }
  }

}
