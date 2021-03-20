using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using NGraphQL.Model;
using NGraphQL.Server;
using System.Linq;

using NGraphQL.Introspection;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  public partial class RequestParser {

    private List<RequestDirective> BuildDirectives(Node dirListNode, DirectiveLocation atLocation, RequestObjectBase parent) {
      var dirList = new List<RequestDirective>();
      if(dirListNode == null)
        return dirList;
      foreach(var dirNode in dirListNode.ChildNodes) {
        var dir = BuildDirective(dirNode, atLocation, parent);
        if(dir == null)
          continue;
        dirList.Add(dir);
      }
      return dirList;
    }

    private RequestDirective BuildDirective(Node dirNode, DirectiveLocation atLocation, RequestObjectBase parent) {
      var dirName = dirNode.ChildNodes[0].ChildNodes[1].GetText(); // child0 is dirName-> @+name
      _path.Push(dirName);
      try {
        var dirDef = LookupDirective(dirName, dirNode);
        if(dirDef == null) 
          return null; // error is already logged
        if(!dirDef.DirInfo.Locations.IsSet(atLocation)) {
          AddError($"Directive @{dirName} may not be placed at this location ({atLocation}). Valid locations: [{dirDef.DirInfo.Locations}].", dirNode);
          return null;
        }
        var dir = new RequestDirective() { Def = dirDef, Location = atLocation, Name = dirName, SourceLocation = dirNode.GetLocation(), Parent = parent };
        var argListNode = dirNode.FindChild(TermNames.ArgListOpt);
        dir.Args = BuildArguments(argListNode.ChildNodes, dir);
        return dir;
      } finally {
        _path.Pop();
      }
    }

  }
}
