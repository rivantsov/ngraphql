using System;
using Irony.Ast;
using Irony.Parsing;

namespace NGraphQL {
  public class GraphQLSchemaGrammar : Grammar {

    public GraphQLSchemaGrammar() : base(caseSensitive: true) {
      AstNodeCreator createNode = null;

      // special chars
      var comma = ToTerm(",", "comma");
      NonGrammarTerminals.Add(comma); //comma is ignored in GraphQL, just like comments
      var colon = ToTerm(":", "colon");
      var excl = ToTerm("!");
      var nonNullOpt = new NonTerminal("nonNull", Empty | excl);
      var pipe = ToTerm("|");
      var pipeOpt = new NonTerminal("pipeOpt", Empty | pipe);
      var amp = ToTerm("&");
      var ampOpt = new NonTerminal("ampOpt", Empty | amp);
      var ellipsis = ToTerm("...");
      var extend = ToTerm("extend");


      //terminals
      var lineTerminators = new string[] { "\r", "\n", "\u2085", "\u2028", "\u2029" };
      var comment = new CommentTerminal("comment", "#", lineTerminators);
      var number = new NumberLiteral("number");
      var tripleQt = "\"\"\"";
      var strSimple = new StringLiteral("strSimple", "\"", StringOptions.AllowsUEscapes, createNode);
      var strBlock = new StringLiteral("strBlock", tripleQt, StringOptions.AllowsLineBreak | StringOptions.AllowsUEscapes);
      var str = new NonTerminal("str", strSimple | strBlock);
      var nullVal = ToTerm("null");

      var name = new IdentifierTerminal("name");
      var varName = new IdentifierTerminal("varName");
      varName.AllFirstChars = "$";
      var dirName = new IdentifierTerminal("dirName");
      dirName.AllFirstChars = "@";
      var enumVal = new NonTerminal("enumVal", name);

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
      var rootOpDefList = new NonTerminal("rootOpDefList");
      var rootOpDef = new NonTerminal("rootOpDef");

      var opDef = new NonTerminal("opDef");
      var fragmDef = new NonTerminal("fragmDef");
      var typeCond = new NonTerminal("typeCond");
      var typeCondOpt = new NonTerminal("typeCondOpt");

      var typeDef = new NonTerminal("typeDef");
      var scalarTypeDef = new NonTerminal("scalarTypeDef");
      var objTypeDef = new NonTerminal("objTypeDef");
      var intfTypeDef = new NonTerminal("intfTypeDef");
      var unionTypeDef = new NonTerminal("unionTypeDef");
      var enumTypeDef = new NonTerminal("enumTypeDef");
      var opTypeDef = new NonTerminal("opTypeDef");
      var opTypeDefList = new NonTerminal("opTypeDefList");

      var inputObjTypeDef = new NonTerminal("inputObjTypeDef");
      var descrOpt = new NonTerminal("descrOpt", Empty | str);
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
      var unionMemberTypes = new NonTerminal("unionMemberTypes");
      var unionList = new NonTerminal("unionList");
      var enumValDefsOpt = new NonTerminal("enumValDefsOpt");
      var enumValDefList = new NonTerminal("enumValDefList");
      var enumValDef = new NonTerminal("enumValDef");
      var inputFldDefsOpt = new NonTerminal("inputFldDefsOpt");
      var inputFldDefList = new NonTerminal("inputFldDefList");
      var inputFldDef = new NonTerminal("inputFldDef");
      var fragmSpread = new NonTerminal("fragmSpread");
      var inlineFragm = new NonTerminal("inlineFragm");

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

      // TypeExt
      var scalarTypeExt = new NonTerminal("scalarTypeExt");
      var objTypeExt = new NonTerminal("objTypeExt");
      var intfTypeExt = new NonTerminal("intfTypeExt");
      var unionTypeExt = new NonTerminal("unionTypeExt");
      var enumTypeExt = new NonTerminal("enumTypeExt");
      var inputObjTypeExt = new NonTerminal("inputObjTypeExt");

      var defaultTopQuery = new NonTerminal("defaultTopQuery");
      var defs = new NonTerminal("defs");



      // Document
      gqlDoc.Rule = defs | defaultTopQuery;
      defs.Rule = MakePlusRule(defs, def);
      def.Rule = execDef | typeSysDef | typeSysExt;
      execDef.Rule = opDef | fragmDef; 
      typeSysDef.Rule = schemaDef | typeDef | dirDef;
      typeSysExt.Rule = schemaExt | typeExt;
      defaultTopQuery.Rule = selSet; 

      //schemaDef
      schemaDef.Rule = "schema" + dirList + "{" + rootOpDefList + "}";
      rootOpDefList.Rule = MakePlusRule(rootOpDefList, rootOpDef);
      rootOpDef.Rule = opType + colon + name;

      // opDef
      opDef.Rule = opType + nameOpt + varDefsOpt + dirList + selSet;
      opType.Rule = ToTerm("query") | "mutation" | "subscription";

      //typeDef
      typeDef.Rule = scalarTypeDef | objTypeDef | intfTypeDef | unionTypeDef | enumTypeDef | inputObjTypeDef;
      scalarTypeDef.Rule = descrOpt + "scalar" + name + dirList;
      objTypeDef.Rule = descrOpt + "type" + name + implsIntfsOpt + dirList + fldsDefOpt;
      intfTypeDef.Rule = descrOpt + "interface" + name + dirList + fldsDefOpt;
      unionTypeDef.Rule = descrOpt + "union" + name + dirList + unionMemberTypes;
      enumTypeDef.Rule = descrOpt + "enum" + name + dirList + enumValDefsOpt;
      inputObjTypeDef.Rule = descrOpt + "input" + name + dirList + inputFldDefsOpt;

      // union members
      unionMemberTypes.Rule = Empty | "=" + pipeOpt + unionList;
      unionList.Rule = MakePlusRule(unionList, pipe, name);

      //fragmDef
      fragmDef.Rule = "fragment" + name + typeCond + dirList + selSet; // name should NOT be 'on' - we make it reserved word

      //typeCond
      typeCond.Rule = "on" + name;
      typeCondOpt.Rule = Empty | typeCond;

      // enum members
      enumValDefsOpt.Rule = Empty | "{" + enumValDefList + "}";
      enumValDefList.Rule = MakePlusRule(enumValDefList, enumValDef);
      enumValDef.Rule = descrOpt + enumVal + dirList; 

      // implements clause
      implsIntfsOpt.Rule = Empty | "implements" + ampOpt + intfList;
      intfList.Rule = MakePlusRule(intfList, amp, name);

      // inputFldDefs
      inputFldDefsOpt.Rule = Empty | "{" + inputFldDefList + "}";
      inputFldDefList.Rule = MakePlusRule(inputFldDefList, inputFldDef);
      inputFldDef.Rule = descrOpt + name + colon + typeRef + dftValueOpt + dirList;

      // fields
      fldsDefOpt.Rule = Empty | "{" + fldDefList + "}";
      fldDefList.Rule = MakeStarRule(fldDefList, fldDef);
      fldDef.Rule = descrOpt + name + argDefsOpt + colon + typeRef + dirList;
      fld.Rule = fldFullName + argsOpt + dirList + selSetOpt;
      fldFullName.Rule = name | name + colon + name;

      //directives
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
      dirList.Rule = MakeStarRule(dirList, dir);
      dir.Rule = dirName + argsOpt;

      // varDefs
      varDefsOpt.Rule = Empty | "(" + varDefList + ")";
      varDefList.Rule = MakePlusRule(varDefList, varDef);
      varDef.Rule = varName + colon + typeRef + dftValueOpt;
      dftValueOpt.Rule = Empty | "=" + val;

      typeRef.Rule = name + nonNullOpt | listType + nonNullOpt;
      listType.Rule = "[" + typeRef + "]";
      
      // args
      argsOpt.Rule = Empty | "(" + argList + ")";
      argList.Rule = MakeStarRule(argList, arg);
      arg.Rule = name + colon + val;

      // SelectionSet
      selSet.Rule = "{" + selList + "}";
      selSetOpt.Rule = selSet | Empty; 
      selList.Rule = MakePlusRule(selList, sel);
      sel.Rule = fld | fragmSpread | inlineFragm;

      // fragments
      fragmSpread.Rule = ellipsis + name + dirList;
      inlineFragm.Rule = ellipsis + typeCondOpt + dirList + selSet;  

      // Value/Literal
      val.Rule = varName | number | str | boolVal | nullVal | enumVal | listVal | objVal;
      listVal.Rule = "[" + valList + "]";
      valList.Rule = MakeStarRule(valList, val);
      objVal.Rule = "{" + objFldList + "}";
      objFldList.Rule = MakePlusRule(objFldList, objFld);
      objFld.Rule = name + colon + val;

      // schemaExt
      schemaExt.Rule = extend + "schema" + dirList + "{" + opTypeDefList + "}";
      opTypeDefList.Rule = MakePlusRule(opTypeDefList, opTypeDef);
      opTypeDef.Rule = opType + colon + name;

      // TypeExt
      typeExt.Rule = scalarTypeExt | objTypeExt | intfTypeExt | unionTypeExt | enumTypeExt | inputObjTypeExt;
      scalarTypeExt.Rule = extend + "scalar" + name + dirList;
      objTypeExt.Rule = extend + "type" + name + implsIntfsOpt + dirList + fldsDefOpt;
      intfTypeExt.Rule = extend + "interface" + name + dirList + fldsDefOpt;
      unionTypeExt.Rule = extend + "union" + name + dirList + unionMemberTypes;
      enumTypeExt.Rule = extend + "enum" + name + dirList + enumValDefsOpt;
      inputObjTypeExt.Rule = extend + "input" + name + dirList + inputFldDefsOpt;


      this.MarkReservedWords("on", "type", "interface", "enum", "union", "true", "false");
      this.RegisterBracePair("(", ")");
      this.RegisterBracePair("[", "]");
      this.RegisterBracePair("{", "}");
      this.MarkPunctuation(":", ",", "{", "}", "(", ")", "[", "]");
      this.MarkTransient(val, selSetOpt, nonNullOpt, enumVal);
      this.Root = gqlDoc; 
    }

  } //class
}
