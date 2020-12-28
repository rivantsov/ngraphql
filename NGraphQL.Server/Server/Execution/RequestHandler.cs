using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Request;
using NGraphQL.Server;

namespace NGraphQL.Server.Execution {

  public class RequestHandler {
    GraphQLServer _server; 
    RequestContext _requestContext;
    bool _parallelQuery = true;

    public RequestHandler(GraphQLServer server,  RequestContext requestContext) {
      _server = server; 
      _requestContext = requestContext;
    }

    public async Task ExecuteAsync() {
      if (!ParseBuildRequest())
        return;
      AssignRequestOperation();
      if (_requestContext.Failed)
        return;

      // signal to prepare/deserialize variables; in http/web scenario, we cannot parse variables immediately -
      // we do not know variable types yet. Now that we have parsed and prepared query, we have var types;
      // we fire this event, parent GraphQLHttpServer will handle it and deserializer variables. 
      _server.Events.OnRequestPrepared(_requestContext); 
      BuildOperationVariables();
      if(_requestContext.Failed)
        return;

      var op = _requestContext.Operation; 
      var opType = op.OperationType;
      var topScope = new OutputObjectScope(); // we do not count it as an 'output object', so no incr of object count in metrics
      _requestContext.Response.Data = topScope;
      await ExecuteOperationAsync(_requestContext.Operation, topScope);
    }

    private async Task ExecuteOperationAsync(GraphQLOperation op, OutputObjectScope topScope) {
      var opOutItemSet = op.SelectionSubset.MappedItemSets.FirstOrDefault(fi => fi.ObjectTypeDef == op.OperationTypeDef);
      var topFields = _requestContext.GetIncludedMappedFields(opOutItemSet);
      topScope.Init(op.OperationTypeDef, topFields);
      var parallel = _parallelQuery && op.OperationType == OperationType.Query && topFields.Count > 1; 
      
      // Note: if we go parallel here, note that the topScope is safe for concurrent thread access; 
      //   it is only used to save op result value (SetValue method)
      var executers = new List<OperationFieldExecuter>();
      for(int fieldIndex = 0; fieldIndex < topFields.Count; fieldIndex++) {
        var opExecuter = new OperationFieldExecuter(_requestContext, topScope, fieldIndex);
        executers.Add(opExecuter); 
      }

      _requestContext.Metrics.ExecutionThreadCount = executers.Count; 
      if (parallel)
        await ExecuteAllParallel(executers);
      else
        await ExecuteAllNonParallel(executers); 
    }

    private async Task ExecuteAllParallel(IList<OperationFieldExecuter> executers) {
      var tasks = new List<Task>(); 
      foreach(var exec in executers) {
        var task = Task.Run(() => exec.ExecuteOperationFieldAsync());
        tasks.Add(task);
      }
      await Task.WhenAll(tasks.ToArray());

    }
    private async Task ExecuteAllNonParallel(IList<OperationFieldExecuter> executers) {
      foreach (var exec in executers)
        await exec.ExecuteOperationFieldAsync(); 
    }


    private bool ParseBuildRequest() {
      if (_server.RequestCache.TryLookupParsedRequest(_requestContext))
        return true;

      // Parse
      var parseTree = ParseRequest();
      if (_requestContext.Failed)
        return false;
      // parse/build request
      var reqBuilder = new RequestParser(_requestContext);
      if (!reqBuilder.BuildRequest(parseTree))
        return false;
      // Map and validate
      var mapper = new RequestMapper(_requestContext);
      mapper.MapAndValidateRequest();
      if (_requestContext.Failed)
        return false;

      var success = !_requestContext.Failed;

      if (success && !_requestContext.Metrics.FromCache)
        _server.RequestCache.AddParsedRequest(_requestContext);
      return success; 
    }

    private ParseTree ParseRequest() {
      var text = _requestContext.RawRequest.Query;
      var syntaxParser = _server.Grammar.CreateRequestParser();
      var parseTree = syntaxParser.Parse(text);
      if (parseTree.HasErrors()) {
        // copy errors to response and return
        foreach (var errMsg in parseTree.ParserMessages) {
          var loc = errMsg.Location.ToLocation();
          // we cannot retrieve path here, parser failed early, so no parse tree - this is Irony's limitation, to be fixed
          IList<object> noPath = null;
          var err = new GraphQLError("Query parsing failed: " + errMsg.Message, noPath, loc, ErrorCodes.Syntax);
          _requestContext.AddError(err);
        }
      }
      return parseTree;
    }

    private bool AssignRequestOperation() {
      // validate 
      var ops = _requestContext.ParsedRequest.Operations;
      if (ops.Count == 0) {
        AddBadRequestError($"No operations defined in request.");
        return false;
      }
      // Find the operation to execute
      var opName = _requestContext.RawRequest.OperationName;
      if (string.IsNullOrEmpty(opName)) {
        // No explicit operation name - there must be just one operation in request
        if (ops.Count > 1) {
          AddBadRequestError($"With Operation name not specified, the request must contain a single operation.");
          return false;
        }
        _requestContext.Operation = ops[0];
      } else {
        // Operation name provided - find the operation in the request (it might have more than one)
        _requestContext.Operation = ops.FirstOrDefault(op => op.Name == opName);
        if (_requestContext.Operation == null) {
          AddBadRequestError($"Operation '{opName}' not defined in the request.");
          return false;
        }
      }
      return true;
    }

    private static IDictionary<string, object> _emptyVariableValues = new Dictionary<string, object>();
    
    private void BuildOperationVariables() {
      var varValues = _requestContext.RawRequest.Variables ?? _emptyVariableValues;
      var op = _requestContext.Operation;
      foreach(var varDecl in op.Variables) {
        var inpDef = varDecl.InputDef; 
        if(!varValues.TryGetValue(varDecl.Name, out var rawValue)) {
          var nullable = !inpDef.TypeRef.IsNotNull;
          if(inpDef.HasDefaultValue || nullable)
            rawValue = inpDef.DefaultValue;
          else { 
            AddVariableError($"Value for required variable {varDecl.Name} is not provided.");
            continue;
          }
        }
        if (!TryValidateConvertVarValue(varDecl, rawValue, out var convValue))
          continue; // error added, do other variables  
        var varValue = new VariableValue() { Variable = varDecl, Value = convValue };
        _requestContext.OperationVariables.Add(varValue);
      }
      // TODO: add check that there are no extra variables that are not defined by Op
    }

    private bool TryValidateConvertVarValue(VariableDef varDecl, object rawValue, out object convValue) {
      convValue = rawValue;
      try {
        convValue = _requestContext.ValidateConvert(rawValue, varDecl.InputDef.TypeRef, varDecl);
        return true;
      } catch (AbortRequestException) {
        return false;
      } catch (InvalidInputException bvEx) {
        _requestContext.AddInputError(bvEx);
        return false;
      } catch(Exception ex) {
        var msg = $"Variable ${varDecl.Name}: failed to convert value '{rawValue}' to type {varDecl.InputDef.TypeRef.Name}: " 
                + ex.Message;
        _requestContext.AddInputError(msg, varDecl);
        return false; 
      }
    }

    private GraphQLError AddBadRequestError(string message) {
      var err = new GraphQLError(message, null, QueryLocation.StartLocation, ErrorCodes.BadRequest);
      _requestContext.AddError(err);
      return err;
    }

    private GraphQLError AddVariableError(string message) {
      var err = new GraphQLError(message, null, QueryLocation.StartLocation, ErrorCodes.InputError);
      _requestContext.AddError(err);
      return err;
    }

  }
}
