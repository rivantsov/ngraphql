using System;
using System.Collections.Generic;
using System.Linq;

using Irony.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.Model;
using NGraphQL.Server.RequestModel;
using NGraphQL.Runtime;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  /*
  GraphQL request contains (main body):  
       * a single unnamed query operation
       * OR 1 or more named operations - queries, mutations, subscriptions; with target Operation name specified separately
       * request can contain 0 or more fragment definitions
       * operation name if request contains multiple named operations
       * variable values for variables defined by selected operation
  */

  /// <summary>RequestBuilder builds the request object (tree of request elements) from the syntax tree produced by Irony parser.
  /// This tree is not mapped yet to API model (types, fields to field defs), this is the job of the RequestMapper. </summary>
  public partial class RequestParser {
    public static readonly SourceLocation StartSourceLocation = new SourceLocation() { Line = 1, Column = 1 };

    RequestContext _requestContext;
    Stack<string> _path = new Stack<string>();

    public RequestParser(RequestContext context) {
      _requestContext = context;
    }

    public bool BuildRequest(ParseTree parseTree) {

      _requestContext.ParsedRequest = new ParsedGraphQLRequest();
      var requestDocNode = parseTree.Root;
      var rootTerm = requestDocNode.Term.Name;
      // Find top request elements (operations, fragments) and validate them
      var topItems = GetTopRequestItems(requestDocNode);
      if(!ValidateTopRequestItems(topItems))
        return false;
      // Build operations
      if(topItems.Operations.Count > 0)
        BuildOperations(topItems.Operations);

      // quite if there are any errors
      if(_requestContext.Failed)
        return false;

      if(topItems.DefaultQuery != null)
        BuildDefaultQuery(topItems.DefaultQuery);
      // build fragments
      if(topItems.Fragments.Count > 0)
        BuildFragments(topItems.Fragments);

      return !_requestContext.Failed;
    }

    // In request builder we only map type refs and directives; strictly speaking it belongs to mapper,
    //  but it's just simpler here. It might change in the future
    private TypeDefBase LookupTypeDef(Node typeNode) {
      var typeName = typeNode.GetText();
      var model = _requestContext.ApiModel;
      if(model.TypesByName.TryGetValue(typeName, out var td))
        return td;
      AddError($"Type '{typeName}' not defined.", typeNode);
      return null;
    }

    private DirectiveDef LookupDirective(Node dirNode) {
      var model = _requestContext.ApiModel;
      var dirName = dirNode.ChildNodes[0].GetText();
      if(model.Directives.TryGetValue(dirName, out var dirDef))
        return dirDef;
      AddError($"Directive {dirName} not defined.", dirNode);
      return null;
    }

    private void AddError(string message, ParseTreeNode node) {
      var path = _path.ToArray().Reverse().ToArray();
      var loc = node?.GetLocation() ?? Location.StartLocation;
      var err = new GraphQLError(message, path, loc, ErrorTypes.BadRequest);
      _requestContext.AddError(err);
    }

  }//class
}
