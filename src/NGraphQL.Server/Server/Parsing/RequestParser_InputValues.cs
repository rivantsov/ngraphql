using System;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.Core;
using NGraphQL.Core.Scalars;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Parsing {

  using Node = Irony.Parsing.ParseTreeNode;

  public partial class RequestParser {

    private ValueSource BuildInputValue(Node valueNode, RequestObjectBase parent) {
    
      if (valueNode.Token != null) { //it is simple token
          var tkn = valueNode.Token;
          var tknData = new TokenData() { TermName = tkn.Terminal.Name, Text = tkn.Text, ParsedValue = tkn.Value };
          return new TokenValueSource() { TokenData = tknData, SourceLocation = valueNode.GetLocation(), Parent = parent };
      }

      switch(valueNode.Term.Name) {
        case TermNames.VarName:
          var varName = valueNode.ChildNodes[1].Token.Text; // child0 is $
          return new VariableValueSource() { VariableName = varName, SourceLocation = valueNode.GetLocation(), Parent = parent };

        case TermNames.ConstList:
          var values = valueNode.ChildNodes.Select(n => BuildInputValue(n, parent)).ToArray();
          return new ListValueSource() { Values = values, SourceLocation = valueNode.GetLocation(), Parent = parent };

        case TermNames.ConstInpObj:
          return BuildInputObject(valueNode.ChildNodes[0], parent);
       
        default:
          // never happens; if it ever does, better throw exc than return null
          throw new Exception($"FATAL: Unexpected term  ({valueNode.Term.Name}), input value expected.");
      }
    }

    private ObjectValueSource BuildInputObject(Node fieldsNode, RequestObjectBase parent) {
      var fields = new Dictionary<string, ValueSource>(); 
      foreach(var fldNode in fieldsNode.ChildNodes) {
        var name = fldNode.ChildNodes[0].GetText();
        _path.Push(name); 
        var valueNode = fldNode.ChildNodes[1];
        var fldValue = BuildInputValue(valueNode, parent);
        if(fields.ContainsKey(name))
          AddError($"Duplicate field '{name}'.", valueNode);
        else
          fields.Add(name, fldValue);
        _path.Pop(); 
      }
      return new ObjectValueSource() { SourceLocation = fieldsNode.GetLocation(), Fields = fields, Parent = parent };
    }


  }
}
