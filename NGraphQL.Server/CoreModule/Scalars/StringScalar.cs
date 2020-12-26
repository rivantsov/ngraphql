using System; 
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core.Scalars {

  public class StringScalar : Scalar {

    public StringScalar() : this("String", "String sclar", false) { }

    public StringScalar(string name, string description, bool isCustom) 
        : base(name, description, typeof(string), isCustom) { }

    // The string is actually already parsed by Irony parser (stripped quotes, unescaped, etc),
    //  so here we just return the same value
    public override object ParseToken(RequestContext context, TokenData token) {
      switch(token.TermName) {
        case TermNames.NullValue:
          return null;
        case TermNames.StrSimple:
        case TermNames.StrBlock:
          return token.ParsedValue;
      }
      context.ThrowScalarError($"Invalid text value: '{token.Text}'", token);
      return null;
    }

    const char _dquote = '"';
    const char _backSlash = '\\';
    static char[] _charsToEscape = new char[] { _dquote, _backSlash };

    public override string ToSchemaDocString(object value) {
      if(value == null)
        return "null";
      var strValue = (value is string str) ? str : value.ToString();
      if(strValue.IndexOfAny(_charsToEscape) >= 0)
        strValue = strValue.Replace("\\", "\\\\").Replace("\"", "\\\"");
      return _dquote + strValue + _dquote; 
    }
  }

}
