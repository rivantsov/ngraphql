using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Parsing {

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
      foreach (var op in _requestContext.ParsedRequest.Operations) {
        op.OperationTypeDef = _model.GetOperationDef(op.OperationType);
        MapOperation(op);
        CalcVariableDefaultValues(op);
      }
      _currentOp = null;
    }

    private void AddError(string message, RequestObjectBase item, string errorType = ErrorCodes.BadRequest) {
      var path = item.GetRequestObjectPath();
      var err = new GraphQLError(message, path, item.SourceLocation, errorType);
      _requestContext.AddError(err);
    }

  } // class
}
