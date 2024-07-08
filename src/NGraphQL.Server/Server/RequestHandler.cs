using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Mapping;

namespace NGraphQL.Server {

  public class RequestHandler {
    GraphQLServer _server; 
    RequestContext _requestContext;
    bool _parallelQueryEnabled;

    public RequestHandler(GraphQLServer server,  RequestContext requestContext) {
      _server = server; 
      _requestContext = requestContext;
      _parallelQueryEnabled = server.Settings.Options.IsSet(GraphQLServerOptions.EnableParallelQueries);
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

      BuildDirectiveContexts();

      var opMapping = _requestContext.Operation.OperationTypeDef.Mappings[0];
      var topScope = new OutputObjectScope(new RequestPath(), null, null); 
      _requestContext.Response.Data = topScope;
      await ExecuteOperationAsync(_requestContext.Operation, topScope);
    }

    private void BuildDirectiveContexts() {
      var allDirs = _requestContext.ParsedRequest.AllDirectives;
      if (allDirs.Count == 0)
        return;
      foreach (var dir in allDirs) {
        var argValues = dir.GetArgValues(_requestContext);
        var dirContext = new DirectiveContext() {
          Directive = dir, ArgValues = argValues, RequestContext = _requestContext
        };
        _requestContext.DirectiveContexts.Add(dirContext);
      }
    }

    public async Task ExecuteOperationAsync(GraphQLOperation op, OutputObjectScope topScope) {
      var mappedTopSubset = op.SelectionSubset.MappedSubSets[0];
      var topMappedItems = mappedTopSubset.MappedItems;
      var parallel = _parallelQueryEnabled && op.OperationType == OperationType.Query 
              && op.SelectionSubset.Items.Count > 1; 
      
      var executers = new List<OperationFieldExecuter>();
      foreach(var mappedItem in topMappedItems) {
        switch(mappedItem) {
          case MappedFragmentSpread mfs:
            _requestContext.AddInputError($"Top selection items may not be fragments", mfs.Spread);
            return;
          case MappedSelectionField mappedField:
            var opExecuter = new OperationFieldExecuter(_requestContext, mappedField, topScope);
            executers.Add(opExecuter);
            break; 
        }
      }

      if (parallel)
        await ExecuteAllParallel(executers);
      else
        await ExecuteAllNonParallel(executers);
      
      // Save results from op fields into top scope; we do it here, after all threads finished, to avoid concurrency issues
      // and preserve output field order
      foreach(var ex in executers) {
        topScope.AddValue(ex.ResultKey, ex.Result, ex.MappedField.Resolver.Field.TypeRef);
      }
      // merge fields
      OutputObjectScopeFieldMerger.MergeFields(topScope); 
    }

    private async Task ExecuteAllParallel(IList<OperationFieldExecuter> executers) {
      _requestContext.Metrics.ExecutionThreadCount = executers.Count;
      var tasks = new List<Task>(); 
      foreach(var exec in executers) {
        var task = Task.Run(() => exec.ExecuteOperationFieldAsync());
        tasks.Add(task);
      }
      await Task.WhenAll(tasks.ToArray());

    }
    private async Task ExecuteAllNonParallel(IList<OperationFieldExecuter> executers) {
      _requestContext.Metrics.ExecutionThreadCount = 1;
      foreach (var exec in executers)
        await exec.ExecuteOperationFieldAsync(); 
    }


    private bool ParseBuildRequest() {
      if (_server.RequestCache.TryLookupParsedRequest(_requestContext))
        return true;

      // parse/build request
      var reqBuilder = new RequestParser(_requestContext);
      if (!reqBuilder.ParseRequest())
        return false;

      // Map and validate
      var mapper = new RequestMapper(_requestContext);
      mapper.MapAndValidateRequest();
      if (_requestContext.Failed)
        return false;

      // Validate subscription - only one subscription field, and no variables in selection field args
      if (_requestContext.ParsedRequest.Operations.Any(op => op.OperationType == OperationType.Subscription)) {
        _server.Subscriptions.ValidateParsedRequest(_requestContext);
        if (_requestContext.Failed)
          return false;
      }

      PrepareDirectives(); 

      var success = !_requestContext.Failed;

      if (success && !_requestContext.Metrics.FromCache)
        _server.RequestCache.AddParsedRequest(_requestContext);
      return success; 
    }

    private void PrepareDirectives() {
      var allDirs = _requestContext.ParsedRequest.AllDirectives;
      foreach(var dir in allDirs) {
        dir.Def.Handler.RequestParsed(dir);
      }
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
      var err = new GraphQLError(message, null, SourceLocation.StartLocation, ErrorCodes.BadRequest);
      _requestContext.AddError(err);
      return err;
    }

    private GraphQLError AddVariableError(string message) {
      var err = new GraphQLError(message, null, SourceLocation.StartLocation, ErrorCodes.InputError);
      _requestContext.AddError(err);
      return err;
    }

  }
}
