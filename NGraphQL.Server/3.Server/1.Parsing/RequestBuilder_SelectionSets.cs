using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using NGraphQL.Model;
using NGraphQL.Model.Introspection;
using NGraphQL.Model.Request;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Server.Parsing {
  using Node = ParseTreeNode; 

  public partial class RequestBuilder {

    private List<SelectionItem> BuildSelectionItemsList(Node selSetNode, NamedRequestObject parentItem) {
      var selItems = new List<SelectionItem>();
      foreach(var selNode in selSetNode.ChildNodes) {
        SelectionItem selItem = null;
        switch(selNode.Term.Name) {
          case TermNames.SelField:
            selItem = BuildSelectionField(selNode, parentItem);
            break;
          case TermNames.FragmSpread:
            var name = selNode.ChildNodes[1].GetText();
            selItem = new FragmentSpread() { Name = name, Location = selNode.GetLocation() };
            var dirListNode = selNode.FindChild(TermNames.DirListOpt);
            selItem.Directives = BuildDirectives(dirListNode, DirectiveLocation.Field, selItem);
            break;
          case TermNames.InlineFragm:
            selItem = BuildInlineFragment(selNode);
            break;
        }
        if(selItem != null)
          selItems.Add(selItem);
      }
      return selItems;
    }

    private SelectionField BuildSelectionField(Node selNode, NamedRequestObject parentItem) {
      var selFld = new SelectionField() { Parent = parentItem, Location = selNode.GetLocation() };
      var nameNode = selNode.FindChild(TermNames.AliasedName);
      AssignNameAlias(selFld, nameNode);
      try {
        _path.Push(selFld.Key);
        // arguments
        // the actual arg list is 2 levels below
        var argsListOptNode = selNode.FindChild(TermNames.ArgListOpt);
        // TODO: refactor, this is temp fix
        var argNodes = (argsListOptNode != null) ? argsListOptNode.ChildNodes : new List<ParseTreeNode>();
        selFld.Args = BuildArguments(argNodes, selFld);

        // directives
        var dirListNode = selNode.FindChild(TermNames.DirListOpt);
        selFld.Directives = BuildDirectives(dirListNode, DirectiveLocation.Field, selFld);
        // If the field is an object/interface itself, it should have it's own selection set
        var selSubSetNode = selNode.FindChild(TermNames.SelSet);
        if(selSubSetNode != null && selSubSetNode.ChildNodes.Count > 0) {
          var items = BuildSelectionItemsList(selSubSetNode, selFld);
          selFld.SelectionSubset = new SelectionSubset(selFld, items, selSubSetNode.GetLocation());
        }
        return selFld;
      } finally {
        _path.Pop(); 
      }
    }

    private List<InputValue> BuildArguments(IList<Node> argNodes, NamedRequestObject owner) {
      var args = new List<InputValue>();
      var allNames = new HashSet<string>(); 
      foreach(var argNode in argNodes) {
        var argName = argNode.ChildNodes[0].GetText();
        _path.Push(argName); 
        if (allNames.Contains(argName)) {
          AddError($"Duplicate argument '{argName}'.", argNode);
          continue; 
        }
        allNames.Add(argName);
        var arg = new InputValue() { Name = argName, Parent = owner, Location = argNode.GetLocation() };
        arg.ValueSource = BuildInputValue(argNode.ChildNodes[1], arg);
        args.Add(arg);
        _path.Pop(); 
      }
      return args;
    }

    private void AssignNameAlias(SelectionField fld, ParseTreeNode nameNode) {
      var cn = nameNode.ChildNodes;
      switch(cn.Count) {
        case 1:
          fld.Name = cn[0].GetText();
          return;
        case 2:
          fld.Alias = cn[0].GetText();
          fld.Name = cn[1].GetText();
          return;
      }
    }


  }
}
