using System;
using System.Collections.Generic;
using System.Linq;

using Irony.Parsing;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  public partial class RequestParser {

   // nested class TopRequestItems
    class TopRequestItems {
      public ParseTreeNode DefaultQuery;
      public List<ParseTreeNode> Fragments = new List<ParseTreeNode>();
      public List<ParseTreeNode> Operations = new List<ParseTreeNode>();
      // this is 'defaultQuery', there should be just one. Grammar allows multiple, but we catch it 
      // when we validate queries
      public List<ParseTreeNode> AnonymousQueries = new List<ParseTreeNode>();
    }

    private TopRequestItems GetTopRequestItems(ParseTreeNode requestDoc) {
      var topItems = new TopRequestItems();
      // query contains operation(s), anonymous ops, and fragment definitions
      var elemListNode = requestDoc.FindChild(TermNames.RequestElemList);
      if(elemListNode == null)
        return topItems; 
      foreach(var node in elemListNode.ChildNodes) {
        switch(node.Term.Name) {
          case TermNames.FragmDef:
            topItems.Fragments.Add(node); 
            break;
          case TermNames.RequestOp:
            topItems.Operations.Add(node); 
            break;
          case TermNames.DefaultQuery:
            topItems.AnonymousQueries.Add(node); 
            break; 
        }
      }
      return topItems;
    }

    private bool ValidateTopRequestItems(TopRequestItems topItems) {
      // If there is a default query, it should be single one, and there should be no other ops in the request, only fragments;
      // and opName should not be specified
      if (topItems.AnonymousQueries.Count > 1) {
        var aq1 = topItems.AnonymousQueries[1];
        AddError("The request may not contain more than one default (anonymous) query", aq1);
        return false;
      }
      if(topItems.AnonymousQueries.Count > 0) {
        topItems.DefaultQuery = topItems.AnonymousQueries[0];
        if(topItems.Operations.Count > 0) {
          var op0 = topItems.Operations[0];
          AddError("If the request contains a default (anonymous) query, it cannot contain any other operations.", op0);
          return false;
        }
      }
      if (topItems.DefaultQuery == null) {
        if (topItems.Operations.Count == 0) {
         AddError("The query contains no default query or operations.", null);
          return false;
        }
      }

      return true; 
    }
    private void BuildOperations(IList<Node> opNodes) {
      foreach(var opNode in opNodes) {
        var nameNode = opNode.FindChild(TermNames.Name);
        string name = nameNode?.GetText() ?? null;
        var opTypeNode = opNode.FindChild(TermNames.OpType);
        var opTypeStr = opTypeNode.GetText();
        if(!Enum.TryParse<OperationType>(opTypeStr, true, out var opType)) {
          AddError($"Invalid operation type '{opTypeStr}'.", opTypeNode);
          continue;
        }
        var op = new GraphQLOperation() { Name = name, OperationType = opType };
        CompleteBuildOperation(op, opNode);
      }
    }

    private void BuildDefaultQuery(Node qNode) {
      var query = new GraphQLOperation() { Name = null, OperationType = OperationType.Query };
      CompleteBuildOperation(query, qNode);
    }

    private void CompleteBuildOperation(GraphQLOperation op, Node opNode) {
      try {
        if(op.Name != null)
          _path.Push(op.Name);
        var varDefsNode = opNode.FindChild(TermNames.VarDefList);
        op.Variables = BuildOperationVariables(varDefsNode);
        var selSetNode = opNode.FindChild(TermNames.SelSet);
        if (selSetNode != null) {
          var selItems = BuildSelectionItemsList(selSetNode, op);
          op.SelectionSubset = new SelectionSubset(op, selItems, selSetNode.GetLocation());
        }
        _requestContext.ParsedRequest.Operations.Add(op);
      } finally {
        // op.Name might have been assigned, so we don't check name, but just pop path
        _path.Clear(); 
      }
    }

    static TypeKind[] _allowedVarTypeKinds = new TypeKind[]
              { TypeKind.Scalar, TypeKind.Enum, TypeKind.InputObject };

    private IList<VariableDef> BuildOperationVariables(Node varDefsNode) {
      if(varDefsNode == null)
        return VariableDef.EmptyList;

      var varList = new List<VariableDef>();
      foreach(var vn in varDefsNode.ChildNodes) {
        var name = vn.ChildNodes[0].GetText();
        // remove $ prefix; we define/track var name as a token without $; in Http/Json request variables are referenced without prefix; 
        //     see https://graphql.org/learn/serving-over-http/#post-request
        name = name.Substring(1); 
        var typeRef = BuildTypeReference(vn.ChildNodes[1]);
        if(typeRef == null)
          continue; // error already posted
        var typeDef = typeRef.TypeDef;
        if(!_allowedVarTypeKinds.Contains(typeDef.Kind)) {
          AddError($"Invalid variable type ( {name}: {typeRef.Name}). Only scalar, enum or input types are allowed.", vn);
          continue;
        }
        var inpDef = new InputValueDef() { Name = name, TypeRef = typeRef };
        var varDef = new VariableDef() { Name = name, InputDef = inpDef, Location = vn.GetLocation() };
        // check default value
        if (vn.ChildNodes.Count > 2)
          varDef.ParsedDefaultValue = BuildInputValue(vn.ChildNodes[2], varDef);
        // directives
        var dirListNode = vn.FindChild(TermNames.DirListOpt);
        if (dirListNode != null)
          varDef.DirectiveRefs = BuildDirectives(dirListNode, DirectiveLocation.VariableDefinition, varDef);
        varList.Add(varDef);
      }
      return varList;
    }

  } //class
}
