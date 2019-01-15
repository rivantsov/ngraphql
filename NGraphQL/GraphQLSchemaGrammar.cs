using System;
using Irony.Ast;
using Irony.Parsing;

namespace NGraphQL {
  public class GraphQLSchemaGrammar : Grammar {

    public GraphQLSchemaGrammar() : base(caseSensitive: true) {
      AstNodeCreator createNode = null;

      // special chars
      var comma = ToTerm(",", "comma");
      NonGrammarTerminals.Add(comma);
      var colon = ToTerm(":", "colon");
      var excl = ToTerm("!");
      var nonNullOpt = new NonTerminal("nonNull", Empty | excl);
      var pipe = ToTerm("|");
      var amp = ToTerm("&");
      var ampOpt = new NonTerminal("ampOpt", Empty | amp);


      //terminals
      var lineTerminators = new string[] { "\r", "\n", "\u2085", "\u2028", "\u2029" };
      var comment = new CommentTerminal("comment", "#", lineTerminators);
      var number = new NumberLiteral("number");
      var tripleQt = "\"\"\"";
      var strSimple = new StringLiteral("str", "\"", StringOptions.AllowsUEscapes, createNode);
      var strBlock = new StringLiteral("strBlock", tripleQt,
                               StringOptions.AllowsLineBreak | StringOptions.AllowsUEscapes, createNode);
      strBlock.ValidateToken += BlockString_ValidateToken;
      var nullVal = ToTerm("null");

      var name = new IdentifierTerminal("name");
      var varName = new IdentifierTerminal("varName");
      varName.AllFirstChars = "$";
      var dirName = new IdentifierTerminal("dirName");
      dirName.AllFirstChars = "@";
      var enumVal = new NonTerminal("enumVal", name);
      MarkTransient(enumVal);

      this.NonGrammarTerminals.Add(comment);

      //non-terminals
      var gqlDoc = new NonTerminal("doc");
      var def = new NonTerminal("def");
      var execDef = new NonTerminal("execDef");
      var typeSysDef = new NonTerminal("typeSystemDef");
      var typeSysExt = new NonTerminal("typeSysExt");
      var schemaDef = new NonTerminal("schemaDef");
      var dirDef = new NonTerminal("dirDef");
      var schemaExt = new NonTerminal("schemaExt");
      var rootOpDefs = new NonTerminal("rootOpDefs");
      var rootOpDef = new NonTerminal("rootOpDef");

      var opDef = new NonTerminal("opDef");
      var fragmDef = new NonTerminal("fragmDef");
      var typeCond = new NonTerminal("typeCond");

      var typeDef = new NonTerminal("typeDef");
      var scalarTypeDef = new NonTerminal("scalarTypeDef");
      var objTypeDef = new NonTerminal("objTypeDef");
      var intfTypeDef = new NonTerminal("intfTypeDef");
      var unionTypeDef = new NonTerminal("unionTypeDef");
      var enumTypeDef = new NonTerminal("enumTypeDef");
      var inputObjTypeDef = new NonTerminal("inputObjTypeDef");
      var descrOpt = new NonTerminal("descrOpt", Empty | strSimple | strBlock);
      var argDefList = new NonTerminal("argDefList");
      var inputValueDef = new NonTerminal("inputValueDef");
      var listType = new NonTerminal("listType");
      var dirLoc = new NonTerminal("dirLoc");
      var execDirLoc = new NonTerminal("execDirLoc");
      var typeDirLoc = new NonTerminal("typeDirLoc");
      var implsIntfsOpt = new NonTerminal("implsIntfsOpt");
      var fldsDefOpt = new NonTerminal("fldsDefOpt");
      var fldDefList = new NonTerminal("fldDefList");
      var fldDef = new NonTerminal("fldDef");
      var intfList = new NonTerminal("intfList");



      var argDefsOpt = new NonTerminal("argDefsOpt");
      var dirLocs = new NonTerminal("dirLocs");

      var typeExt = new NonTerminal("typeExt");
      var opType = new NonTerminal("opType");
      var nameOpt = new NonTerminal("nameopt", name | Empty);
      var typeRef = new NonTerminal("typeRef");
      var dftValueOpt = new NonTerminal("dftValueOpt");
      var val = new NonTerminal("val");
      var arg = new NonTerminal("arg");
      var argList = new NonTerminal("argList");
      var argsOpt = new NonTerminal("argsOpt");
      var listVal = new NonTerminal("listVal");
      var valList = new NonTerminal("valList");
      var objVal = new NonTerminal("objVal");
      var objFldList = new NonTerminal("objFldList");
      var objFld = new NonTerminal("objFld");
      var boolVal = new NonTerminal("boolVal", ToTerm("true") | "false");

      var varDef = new NonTerminal("varDef");
      var varDefList = new NonTerminal("varDefList");
      var varDefsOpt = new NonTerminal("varDefsOpt");

      var selList = new NonTerminal("selList");
      var selSet = new NonTerminal("selSet");
      var selSetOpt = new NonTerminal("selSetOpt");
      var sel = new NonTerminal("sel");
      var fld = new NonTerminal("fld");
      var fldFullName = new NonTerminal("fldFullName");

      var dir = new NonTerminal("dir");
      var dirList = new NonTerminal("dirList");

      // Document
      gqlDoc.Rule = MakePlusRule(gqlDoc, def);
      def.Rule = execDef | typeSysDef; // | typeSysExt;
      execDef.Rule = opDef | fragmDef;
      typeSysDef.Rule = schemaDef | typeDef | dirDef;
      typeSysExt.Rule = schemaExt | typeExt;

      // opDef
      opDef.Rule = opType + nameOpt + varDefsOpt + dirList + selSet;
      opType.Rule = ToTerm("query") | "mutation" | "subscription";

      //fragmDef
      fragmDef.Rule = "fragment" + name + typeCond + dirList + selSet; // name should NOT be 'on' - add extra cond?
      typeCond.Rule = "on" + name;

      //schemaDef
      schemaDef.Rule = "schema" + dirList + "{" + rootOpDefs + "}";
      rootOpDefs.Rule = MakePlusRule(rootOpDefs, rootOpDef);
      rootOpDef.Rule = opType + colon + name;

      //typeDef
      typeDef.Rule = scalarTypeDef | objTypeDef; // | intfTypeDef | unionTypeDef | enumTypeDef | inputObjTypeDef;
      scalarTypeDef.Rule = descrOpt + "scalar" + name + dirList;
      objTypeDef.Rule = descrOpt + "type" + name + implsIntfsOpt + dirList + fldsDefOpt;
      implsIntfsOpt.Rule = Empty | "implements" + ampOpt + intfList;
      intfList.Rule = MakePlusRule(intfList, amp, name);
      fldsDefOpt.Rule = Empty | "{" + fldDefList + "}";
      fldDefList.Rule = MakeStarRule(fldDefList, fldDef);
      fldDef.Rule = descrOpt + name + argDefsOpt + colon + typeRef + dirList; 


      //dirDef
      dirDef.Rule = descrOpt + "directive" + dirName + argDefsOpt + "on" + dirLocs;
      argDefsOpt.Rule = Empty | "(" + argDefList + ")";
      argDefList.Rule = MakeStarRule(argDefList, inputValueDef);
      inputValueDef.Rule = descrOpt + name + colon + typeRef + dftValueOpt + dirList;
      dirLocs.Rule = MakeListRule(dirLocs, pipe, dirLoc);
      dirLoc.Rule = execDirLoc | typeDirLoc;
      execDirLoc.Rule = ToTerm("QUERY") | "MUTATION" | "SUBSCRIPTION" | "FIELD" | "FRAGMENT_DEFINITION" | 
                           "FRAGMENT_SPREAD" | "INLINE_FRAGMENT";
      typeDirLoc.Rule = ToTerm("SCHEMA") | "SCALAR" | "OBJECT" | "FIELD_DEFINITION" | "ARGUMENT_DEFINITION" |
            "INTERFACE" | "UNION" | "ENUM" | "ENUM_VALUE" | "INPUT_OBJECT" | "INPUT_FIELD_DEFINITION"; 

      // varDefs
      varDefsOpt.Rule = Empty | "(" + varDefList + ")";
      varDefList.Rule = MakePlusRule(varDefList, varDef);
      varDef.Rule = varName + ":" + typeRef + dftValueOpt;
      dftValueOpt.Rule = Empty | "=" + val;


      typeRef.Rule = name + nonNullOpt | listType + nonNullOpt;
      listType.Rule = "[" + typeRef + "]";
      
      dirList.Rule = MakeStarRule(dirList, dir);
      dir.Rule = dirName + argsOpt;
      argsOpt.Rule = Empty | "(" + argList + ")";
      argList.Rule = MakeStarRule(argList, arg);
      arg.Rule = name + ":" + val;

      selSet.Rule = "{" + selList + "}";
      selSetOpt.Rule = selSet | Empty; 
      selList.Rule = MakePlusRule(selList, sel);
      sel.Rule = fld; // | fragmSpread | inlineFragm
      fld.Rule = fldFullName + argsOpt + dirList + selSetOpt;
      fldFullName.Rule = name | name + ":" + name;

      // Value/Literal
      val.Rule = varName | number | strSimple | strBlock | boolVal | nullVal | enumVal | listVal | objVal;
      listVal.Rule = "[" + valList + "]";
      valList.Rule = MakeStarRule(valList, val);
      objVal.Rule = "{" + objFldList + "}";
      objFldList.Rule = MakePlusRule(objFldList, objFld);
      objFld.Rule = name + colon + val;



      this.MarkPunctuation(":", ",", "{", "}", "(", ")", "[", "]");
      this.MarkTransient(val, selSetOpt, nonNullOpt);
      this.Root = gqlDoc; 
    }

    private void BlockString_ValidateToken(object sender, ValidateTokenEventArgs e) {
      if(!e.Token.Text.EndsWith("\\"))
        return; 
      // to do - check handling of \""" token (escaped block string terminator
    }

  } //class
}
