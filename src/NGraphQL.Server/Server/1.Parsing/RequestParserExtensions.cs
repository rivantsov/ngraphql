﻿using System;
using System.Linq;

using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  internal static class RequestParserExtensions {

    public static ParseTreeNode FindChild(this ParseTreeNode node, string termName) {
      var child = node.ChildNodes.FirstOrDefault(c => c.Term.Name == termName);
      return child;
    }

    public static string GetText(this ParseTreeNode node) {
      if(node.Token != null)
        return node.Token.ValueString;
      // try single child node if any
      if(node.ChildNodes.Count == 1)
        return node.ChildNodes[0].GetText();
      // something really wrong internally if we are here
      throw new Exception($"FATAL: Node '{node.Term.Name}' is not a terminal, and has childCount <> 1; GetText() failed.");
    }

    public static string GetDescription(this ParseTreeNode node) {
      var descrNode = node.FindChild(TermNames.DescrOpt);
      return descrNode?.GetText();
    }


    public static FieldDef FindField(this ComplexTypeDef fieldSet, string name) {
      var fld = fieldSet.Fields.FirstOrDefault(f => f.Name == name);
      return fld;
    }


    public static bool IsOneOf(this TypeDefBase typeDef, params TypeKind[] kinds) {
      for(int i = 0; i < kinds.Length; i++)
        if(typeDef.Kind == kinds[i])
          return true;
      return false;
    }


    public static SourceLocation GetLocation(this Node node) {
      if(node == null)
        return SourceLocation.StartLocation;
      return node.Span.Location.ToLocation();
    }

    public static SourceLocation ToLocation(this Irony.Parsing.SourceLocation srcLoc) {
      // somehow Irony's location line and column are zero based
      return new SourceLocation() { Line = srcLoc.Line + 1, Column = srcLoc.Column + 1 };
    }




  }
}
