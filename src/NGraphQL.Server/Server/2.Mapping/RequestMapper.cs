using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Mapping {

  /// <summary>RequestMapper takes request tree and maps its objects to API model; for ex: selection field is mapped to field definition</summary>
  public partial class RequestMapper {
    GraphQLApiModel _model;
    RequestContext _requestContext;
    GraphQLOperation _currentOp;

    public RequestMapper(RequestContext requestContext) {
      _requestContext = requestContext;
      _model = _requestContext.ApiModel;
    }

    public void MapAndValidateRequest() {
      var fragmAnalyzer = new FragmentAnalyzer(_requestContext);
      fragmAnalyzer.Analyze();
      if (_requestContext.Failed)
        return;
      foreach(var fragm in _requestContext.ParsedRequest.Fragments) {
        if (!fragm.IsInline)
          MapFragment(fragm); 
      }

      foreach (var op in _requestContext.ParsedRequest.Operations) {
        if (!AssignOperationDef(op))
          continue; 
        MapOperation(op);
        CalcVariableDefaultValues(op);
      }
      _currentOp = null;
    }

    private bool AssignOperationDef(GraphQLOperation op) {
      ObjectTypeDef opDef = null;
      switch (op.OperationType) {
        case OperationType.Query: opDef = _model.QueryType; break;
        case OperationType.Mutation: opDef = _model.MutationType; break;
        case OperationType.Subscription: opDef = _model.SubscriptionType; break;
      }
      if (opDef == null) {
        AddError($"Operation '{op.OperationType}' is not defined in schema. Operation: '{op.Name}'.", op);
        return false; 
      }
      op.OperationTypeDef = opDef; 
      return true;
    }

    private void MapFragment(FragmentDef fragm) {
      MapSelectionSubSet(fragm.SelectionSubset, fragm.OnTypeRef.TypeDef);
    }

    private void AddError(string message, RequestObjectBase item, string errorType = ErrorCodes.BadRequest) {
      var path = item.GetRequestObjectPath();
      var err = new GraphQLError(message, path, item.SourceLocation, errorType);
      _requestContext.AddError(err);
    }

  } // class
}
