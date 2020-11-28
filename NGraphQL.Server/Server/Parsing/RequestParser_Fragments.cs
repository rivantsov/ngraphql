using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.Model;
using NGraphQL.Model.Introspection;
using NGraphQL.Model.Request;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  partial class RequestParser {

    private void BuildFragments(List<Node> fragmentNodes) {
      var parsedReq = _requestContext.ParsedRequest;
      // first build headers only, so we can handle fragments referencing other fragments
      foreach(var fn in fragmentNodes) {
        var nameNd = fn.FindChild(TermNames.Name);
        var name = nameNd.GetText();
        var fragmDef = new FragmentDef() { Name = name, Location = fn.GetLocation()};
        parsedReq.Fragments.Add(fragmDef);
        // OnType spec
        var typeCondNode = fn.FindChild(TermNames.TypeCond);
        var onTypeNameNode = typeCondNode.ChildNodes[1];
        fragmDef.OnTypeRef = new OnTypeRef() { Location = onTypeNameNode.GetLocation(), Name = onTypeNameNode.GetText(), Parent = fragmDef };
        // selection items
        var selSetNode = fn.FindChild(TermNames.SelSet);
        var items = BuildSelectionItemsList(selSetNode, fragmDef);
        fragmDef.SelectionSubset = new SelectionSubset(fragmDef, items, selSetNode.GetLocation());
        //directives
        var dirListNode = fn.FindChild(TermNames.DirListOpt);
        fragmDef.Directives = BuildDirectives(dirListNode, DirectiveLocation.FragmentDefinition, fragmDef);
      }
    }

    private FragmentSpread BuildInlineFragment(Node selNode) {
      // Create fragment def and fragment spread
      var fragmDefs = _requestContext.ParsedRequest.Fragments;
      var fragmDef = new FragmentDef() { IsInline = true, Name = "_inline_" + fragmDefs.Count };
      fragmDefs.Add(fragmDef);

      // note: On-type is optional, spec mentions this: https://spec.graphql.org/June2018/#sec-Inline-Fragments
      var typeNameNode = selNode.FindChild(TermNames.TypeCondOpt)?.FindChild(TermNames.Name);
      if (typeNameNode != null) {
        var onTypeName = typeNameNode.GetText();
        fragmDef.OnTypeRef = new OnTypeRef() { Name = onTypeName, Location = typeNameNode.GetLocation(), Parent = fragmDef };
      } //if
      // Selection set
      var selSetNode = selNode.FindChild(TermNames.SelSet);
      var items = BuildSelectionItemsList(selSetNode, fragmDef);
      fragmDef.SelectionSubset = new SelectionSubset(fragmDef, items, selSetNode.GetLocation());
      // fragment spread
      var fragm = new FragmentSpread() {
        Fragment = fragmDef, Name = fragmDef.Name, IsInline = true, Location = selNode.GetLocation()
      };
      //Directives
      var dirListNode = selNode.FindChild(TermNames.DirListOpt);
      fragm.Directives = BuildDirectives(dirListNode, DirectiveLocation.InlineFragment, fragm);
      return fragm;
    }

  }
}
