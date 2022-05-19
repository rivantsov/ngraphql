using System;
using Irony.Ast;
using Irony.Parsing;

namespace NGraphQL.Server.Parsing {

  public partial class GraphQLGrammar : Irony.Parsing.Grammar {
    // We build grammar for 2 languages in fact: Schema and Request; the default grammar root is RequestDocRoot, 
    // so default parser is for parsing requests. We register an alternative root for schema, and use it to create a parser 
    // for schema docs. 
    public readonly NonTerminal SchemaDocRoot;
    public readonly NonTerminal RequestDocRoot; 

    // Constructor
    public GraphQLGrammar() : base(caseSensitive: true) {

      // special chars
      var comma = ToTerm(",", "comma");
      NonGrammarTerminals.Add(comma); //comma is ignored in GraphQL, just like comments
      var colon = ToTerm(":", "colon");
      var excl = ToTerm("!");
      var pipe = ToTerm("|");
      var pipeOpt = new NonTerminal("pipeOpt", Empty | pipe);
      var amp = ToTerm("&");
      var ampOpt = new NonTerminal("ampOpt", Empty | amp);
      var ellipsis = ToTerm("...");
      var extend = ToTerm("extend");

      // comments
      var lineTerminators = new string[] { "\r", "\n", "\u2085", "\u2028", "\u2029" };
      var comment = new CommentTerminal("comment", "#", lineTerminators);
      this.NonGrammarTerminals.Add(comment);

      //terminals
      var number = new NumberLiteral(TermNames.Number, NumberOptions.AllowSign);
      number.DefaultIntTypes = new TypeCode[] { TypeCode.Int32, TypeCode.Int64, TypeCode.Decimal, NumberLiteral.TypeCodeBigInt };
      number.AddPrefix("0x", NumberOptions.Hex); //allow hex
      var tripleQt = "\"\"\"";
      var strSimple = new StringLiteral(TermNames.StrSimple, "\"", StringOptions.AllowsUEscapes);
      var strBlock = new StringLiteral(TermNames.StrBlock, tripleQt, StringOptions.AllowsLineBreak | StringOptions.AllowsUEscapes);
      var str = new NonTerminal("str", strSimple | strBlock);
      var qStr = new StringLiteral(TermNames.Qstr, "'"); //used by custom scalars: Datetime, Uuid, etc
      var nullVal = ToTerm("null", TermNames.NullValue);
      var name = new IdentifierTerminal(TermNames.Name);

      //non-terminals
      SchemaDocRoot = new NonTerminal("schemaDoc");
      var def = new NonTerminal("def");
      var typeSystemDef = new NonTerminal("typeSystemDef");
      var typeSysExt = new NonTerminal("typeSysExt");
      var schemaDef = new NonTerminal("schemaDef");
      var dirDef = new NonTerminal("dirDef");
      var schemaExt = new NonTerminal("schemaExt");
      var rootOpDefList = new NonTerminal("rootOpDefList");
      var rootOpDef = new NonTerminal("rootOpDef");

      var requestOp = new NonTerminal(TermNames.RequestOp);
      var fragmDef = new NonTerminal(TermNames.FragmDef);
      var typeCond = new NonTerminal(TermNames.TypeCond);
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
      var descrOpt = new NonTerminal(TermNames.DescrOpt, Empty | str);
      var argDefList = new NonTerminal("argDefList");
      var inputValueDef = new NonTerminal("inputValueDef");
      var listTypeRef = new NonTerminal(TermNames.ListTypeRef);
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
      var fragmSpread = new NonTerminal(TermNames.FragmSpread);
      var inlineFragm = new NonTerminal(TermNames.InlineFragm);

      var argDefsOpt = new NonTerminal("argDefsOpt");
      var dirLocs = new NonTerminal("dirLocs");

      var typeExt = new NonTerminal("typeExt");
      var opType = new NonTerminal(TermNames.OpType);
      var nameOpt = new NonTerminal("nameopt", name | Empty);
      var typeRef = new NonTerminal("typeRef");
      var dftValueOpt = new NonTerminal("dftValueOpt");
      var constVal = new NonTerminal("constVal");
      var val = new NonTerminal("val");
      var arg = new NonTerminal("arg");
      var argListOpt = new NonTerminal(TermNames.ArgListOpt);
      var argsOpt = new NonTerminal("argsOpt");
      var constListBr = new NonTerminal("constListBr");
      var constList = new NonTerminal(TermNames.ConstList);
      var constInpObj = new NonTerminal(TermNames.ConstInpObj);
      var constInpObjFldList = new NonTerminal("constInpObjFldList");
      var constObjFld = new NonTerminal("constObjFld");

      var notNullTypeRef = new NonTerminal(TermNames.NotNullTypeRef);
      var nullTypeRef = new NonTerminal("nullTypeRef");
      var baseTypeRef = new NonTerminal(TermNames.BaseTypeRef);

      var trueVal = ToTerm("true", TermNames.True);
      var falseVal = ToTerm("false", TermNames.False);
      var boolVal = new NonTerminal("boolVal", trueVal | falseVal);

      var varName = new NonTerminal(TermNames.VarName, "$" + name);
      var varDef = new NonTerminal("varDef");
      var varDefList = new NonTerminal(TermNames.VarDefList);
      var varDefsOpt = new NonTerminal("varDefsOpt");

      var selSet = new NonTerminal(TermNames.SelSet);
      var selSetBr = new NonTerminal("selSetBr");
      var selSetOpt = new NonTerminal("selSetOpt");
      var selItem = new NonTerminal("selItem");
      var selFld = new NonTerminal(TermNames.SelField);
      var aliasedName = new NonTerminal(TermNames.AliasedName);

      var dirName = new NonTerminal(TermNames.DirName, "@" + name);
      var dir = new NonTerminal("dir");
      var dirListOpt = new NonTerminal(TermNames.DirListOpt);
      var repeatableOpt = new NonTerminal("repeatableOpt", "repeatable" | Empty);

      var scalarTypeExt = new NonTerminal("scalarTypeExt");
      var objTypeExt = new NonTerminal("objTypeExt");
      var intfTypeExt = new NonTerminal("intfTypeExt");
      var unionTypeExt = new NonTerminal("unionTypeExt");
      var enumTypeExt = new NonTerminal("enumTypeExt");
      var inputObjTypeExt = new NonTerminal("inputObjTypeExt");

      var defaultQuery = new NonTerminal(TermNames.DefaultQuery);
      var defs = new NonTerminal("defs");
      var reqElem = new NonTerminal("reqElem");
      var reqElemList = new NonTerminal(TermNames.RequestElemList);
      RequestDocRoot = new NonTerminal(TermNames.RequestDoc);

      // RULES =========================================================================================
      /* 
      According to spec, a request can contain either a single default (anonymous) query, or one or more named operations.
      We could structure the grammar so that it forces this at syntax level, but the problem is unclear error messages
      like 'unexpected symbol' at open brace, and no easy way to improve it. 
      So we structure the grammar to allow multiple default queries, and then catch the error at AST phase.
      */

      // Request Document
      RequestDocRoot.Rule = reqElemList; 
      reqElemList.Rule = MakePlusRule(reqElemList, reqElem);
      reqElem.Rule = requestOp | fragmDef | defaultQuery; 
      requestOp.Rule = descrOpt + opType + nameOpt + varDefsOpt + dirListOpt + selSetBr;
      defaultQuery.Rule = selSetBr; 

      // Schema doc
      SchemaDocRoot.Rule = defs;
      defs.Rule = MakePlusRule(defs, def);
      def.Rule = requestOp | fragmDef | typeSystemDef | typeSysExt;
      typeSystemDef.Rule = schemaDef | typeDef | dirDef;
      typeSysExt.Rule = schemaExt | typeExt;

      //schemaDef
      schemaDef.Rule = descrOpt + "schema" + dirListOpt + "{" + rootOpDefList + "}";
      rootOpDefList.Rule = MakePlusRule(rootOpDefList, rootOpDef);
      rootOpDef.Rule = descrOpt + opType + colon + name;

      // opDef
      opType.Rule = ToTerm("query") | "mutation" | "subscription";

      //typeDef
      typeDef.Rule = scalarTypeDef | objTypeDef | intfTypeDef | unionTypeDef | enumTypeDef | inputObjTypeDef;
      scalarTypeDef.Rule = descrOpt + "scalar" + name + dirListOpt;
      objTypeDef.Rule = descrOpt + "type" + name + implsIntfsOpt + dirListOpt + fldsDefOpt;
      intfTypeDef.Rule = descrOpt + "interface" + name + implsIntfsOpt + dirListOpt + fldsDefOpt;
      unionTypeDef.Rule = descrOpt + "union" + name + dirListOpt + unionMemberTypes;
      enumTypeDef.Rule = descrOpt + "enum" + name + dirListOpt + enumValDefsOpt;
      inputObjTypeDef.Rule = descrOpt + "input" + name + dirListOpt + inputFldDefsOpt;

      // union members
      unionMemberTypes.Rule = Empty | "=" + pipeOpt + unionList;
      unionList.Rule = MakePlusRule(unionList, pipe, name);

      //fragmDef
      fragmDef.Rule = descrOpt + "fragment" + name + typeCond + dirListOpt + selSetBr; 
              // name should NOT be 'on' - we make it reserved word

      //typeCond
      typeCond.Rule = "on" + name;
      // this is intentional (not typeCond); some specifics of AST Builder; maybe refactor it in the future, looks silly
      typeCondOpt.Rule = Empty | "on" + name; 

      // enum members
      enumValDefsOpt.Rule = Empty | "{" + enumValDefList + "}";
      enumValDefList.Rule = MakePlusRule(enumValDefList, enumValDef);
      enumValDef.Rule = descrOpt + name + dirListOpt; 

      // implements clause
      implsIntfsOpt.Rule = Empty | "implements" + ampOpt + intfList;
      intfList.Rule = MakePlusRule(intfList, amp, name);

      // inputFldDefs
      inputFldDefsOpt.Rule = Empty | "{" + inputFldDefList + "}";
      inputFldDefList.Rule = MakePlusRule(inputFldDefList, inputFldDef);
      inputFldDef.Rule = descrOpt + name + colon + typeRef + dftValueOpt + dirListOpt;

      // fields
      fldsDefOpt.Rule = Empty | "{" + fldDefList + "}";
      fldDefList.Rule = MakeStarRule(fldDefList, fldDef);
      fldDef.Rule = descrOpt + name + argDefsOpt + colon + typeRef + dirListOpt;
      selFld.Rule = descrOpt + aliasedName + argsOpt + dirListOpt + selSetOpt;
      aliasedName.Rule = name + colon + name | name; //optional alias

      //directives
      dirDef.Rule = descrOpt + "directive" + dirName + argDefsOpt + repeatableOpt + "on" + pipeOpt + dirLocs;
      argDefsOpt.Rule = Empty | "(" + argDefList + ")";
      argDefList.Rule = MakeStarRule(argDefList, inputValueDef);
      inputValueDef.Rule = descrOpt + name + colon + typeRef + dftValueOpt + dirListOpt;
      dirLocs.Rule = MakeListRule(dirLocs, pipe, dirLoc);
      dirLoc.Rule = execDirLoc | typeDirLoc;
      execDirLoc.Rule = ToTerm("QUERY") | "MUTATION" | "SUBSCRIPTION" | "FIELD" | "FRAGMENT_DEFINITION" | 
                           "FRAGMENT_SPREAD" | "INLINE_FRAGMENT";
      typeDirLoc.Rule = ToTerm("SCHEMA") | "SCALAR" | "OBJECT" | "FIELD_DEFINITION" | "ARGUMENT_DEFINITION" |
            "INTERFACE" | "UNION" | "ENUM" | "ENUM_VALUE" | "INPUT_OBJECT" | "INPUT_FIELD_DEFINITION";
      dirListOpt.Rule = MakeStarRule(dirListOpt, dir);
      dir.Rule = dirName + argsOpt;

      // varDefs
      varDefsOpt.Rule = Empty | "(" + varDefList + ")";
      varDefList.Rule = MakeStarRule(varDefList, varDef); 
      varDef.Rule = varName + colon + typeRef + dftValueOpt + dirListOpt; //directives for vars added in 2020 draft
      dftValueOpt.Rule = Empty | "=" + constVal;

      baseTypeRef.Rule = name; 
      notNullTypeRef.Rule = nullTypeRef + excl;
      nullTypeRef.Rule = listTypeRef | baseTypeRef;  
      typeRef.Rule = baseTypeRef | listTypeRef | notNullTypeRef;
      listTypeRef.Rule = "[" + typeRef + "]";
      
      // args
      argsOpt.Rule = Empty | "(" + argListOpt + ")";
      argListOpt.Rule = MakeStarRule(argListOpt, arg); // might be PLUS according to spec (empty not allowed), but let's be forgiving
      arg.Rule = name + colon + val;

      // SelectionSet
      selSetBr.Rule = "{" + selSet + "}";
      selSetOpt.Rule = selSetBr | Empty; 
      selSet.Rule = MakePlusRule(selSet, selItem);
      selItem.Rule = selFld | fragmSpread | inlineFragm;

      // fragments
      fragmSpread.Rule = ellipsis + name + dirListOpt;
      inlineFragm.Rule = ellipsis + typeCondOpt + dirListOpt + selSetBr;

      // Value/Literal; 'name' is for enum value
      constVal.Rule = number | str | boolVal | nullVal | name | constListBr | constInpObj | qStr;
      val.Rule = varName | constVal;
      constListBr.Rule = "[" + constList + "]";
      constList.Rule = MakeStarRule(constList, val);
      constInpObj.Rule = "{" + constInpObjFldList + "}";
      constInpObjFldList.Rule = MakeStarRule(constInpObjFldList, constObjFld);
      constObjFld.Rule = name + colon + val;

      // schemaExt
      schemaExt.Rule = extend + "schema" + dirListOpt + "{" + opTypeDefList + "}";
      opTypeDefList.Rule = MakePlusRule(opTypeDefList, opTypeDef);
      opTypeDef.Rule = opType + colon + name;

      // TypeExt
      typeExt.Rule = scalarTypeExt | objTypeExt | intfTypeExt | unionTypeExt | enumTypeExt | inputObjTypeExt;
      scalarTypeExt.Rule = extend + "scalar" + name + dirListOpt;
      objTypeExt.Rule = extend + "type" + name + implsIntfsOpt + dirListOpt + fldsDefOpt;
      intfTypeExt.Rule = extend + "interface" + name + dirListOpt + fldsDefOpt;
      unionTypeExt.Rule = extend + "union" + name + dirListOpt + unionMemberTypes;
      enumTypeExt.Rule = extend + "enum" + name + dirListOpt + enumValDefsOpt;
      inputObjTypeExt.Rule = extend + "input" + name + dirListOpt + inputFldDefsOpt;

      this.MarkReservedWords("on", "true", "false");

      this.RegisterBracePair("(", ")");
      this.RegisterBracePair("[", "]");
      this.RegisterBracePair("{", "}");
      this.MarkPunctuation(":", ",", "{", "}", "(", ")", "[", "]", "=", "!");
      
      this.MarkTransient(varDefsOpt, dftValueOpt, reqElem);
      this.MarkTransient(def, typeSystemDef, typeSysExt, typeExt, typeDef, typeRef, fldsDefOpt);
      this.MarkTransient(boolVal, str, val, constListBr, nameOpt, descrOpt, nullTypeRef, constVal);
      this.MarkTransient(selItem, selSetOpt, selSetBr, argsOpt, dirLoc, dirListOpt);

      this.Root = RequestDocRoot;
      this.SnippetRoots.Add(SchemaDocRoot); // alt root for schema doc parser
    }
    
  } //class

}
