using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;

namespace NGraphQL.Server.Parsing {

  // Names of parse nodes (grammar terms) used by the RequestBuilder to find the specific nodes.
  public static class TermNames {
    public const string RequestDoc = "requestDoc";
    public const string RequestOp = "requestOp";
    public const string DefaultQuery = "defaultQuery";
    public const string RequestElemList = "requestElemList";
    public const string OpType = "opType";
    public const string FragmDef = "fragmDef";
    public const string FragmSpread = "fragmSpread";
    public const string InlineFragm = "inlineFragm";

    public const string Name = "name";
    public const string AliasedName = "aliasedName";
    public const string Number = "number";
    public const string VarName = "varName";
    public const string NullValue = "nullVal";
    public const string True = "true";
    public const string False = "false";
    public const string StrSimple = "strSimple";
    public const string StrBlock = "strBlock";
    public const string Qstr = "qStr";

    public const string TypeCond = "typeCond";
    public const string TypeCondOpt = "typeCondOpt";
    public const string ArgListOpt = "argListOpt";
    public const string DirListOpt = "dirListOpt";
    public const string SelSet = "selSet";
    public const string SelField = "selFld";
    public const string VarDefList = "varDefList";
    public const string DescrOpt = "descrOpt";
    public const string ConstList = "constList";
    public const string ConstInpObj = "constInpObj";

    public const string ListTypeRef = "listTypeRef";
    public const string NotNullTypeRef = "notNullTypeRef";
    public const string BaseTypeRef = "baseTypeRef";
  }


}
