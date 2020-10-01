using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;

using NGraphQL.CodeFirst;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model {

  public class ScalarTypeDef : TypeDefBase {
    public readonly bool IsCustom;
    public Type[] CanConvertFrom = new Type[] { }; 

    public ScalarTypeDef(string name, Type clrType, bool isCustom = false) : base(name, TypeKind.Scalar, clrType) {
      IsCustom = isCustom;
      base.TypeRole = SchemaTypeRole.DataType;
    }

    public virtual object ParseToken(RequestContext context, TokenValueSource token) {
      if(token.TokenData.TermName == TermNames.NullValue)
        return null; 
      return token.TokenData.ParsedValue; // this is value converted by Irony parser
    }

    public virtual object ConvertInputValue(object value) {
      return value; 
    }

  }
}
