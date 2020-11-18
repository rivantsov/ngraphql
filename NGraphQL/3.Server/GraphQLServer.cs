using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Construction;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;
using NGraphQL.Utilities;

namespace NGraphQL.Server {

  public class GraphQLServer {
    public readonly GraphQLServerSettings Settings;
    public readonly GraphQLApi Api;
    public GraphQLGrammar Grammar { get; private set; }
    public readonly GraphQLServerEvents Events = new GraphQLServerEvents();
    public readonly RequestCache RequestCache;
    public RequestQuota DefaultRequestQuota = new RequestQuota(); //init with default values
    public IList<string> StartupErrors => _model?.Errors ?? Array.Empty<string>();
    public bool Faulted => StartupErrors.Count > 0; 

    GraphQLApiModel _model;

    public GraphQLServer(GraphQLApi api, GraphQLServerSettings settings = null) {
      Api = api;
      Settings = settings ?? new GraphQLServerSettings();
      RequestCache = new RequestCache(this.Settings);
    }

    public void Initialize() {
      try {
        _model = Api.Model = new GraphQLApiModel(Api);
        var modelBuilder = new ModelBuilder(Api);
        modelBuilder.BuildModel();
        Grammar = new GraphQLGrammar();
        Grammar.Init();
        // call init on all types
        foreach (var typeDef in _model.Types)
          typeDef.Init(this);
      } catch (Exception ex) {
        _model.Errors.Add(ex.ToText());
      }
      if (_model.Errors.Count > 0) {
        Trace.WriteLine("API model errors: \r\n" + string.Join(Environment.NewLine, _model.Errors));
        throw new ServerStartupException(_model.Errors);
      }
    }
    
    public async Task<GraphQLResponse> ExecuteAsync(GraphQLRequest request) {
      var context = CreateRequestContext(request);
      await ExecuteRequestAsync(context);
      return context.Response;
    }

    public RequestContext CreateRequestContext(GraphQLRequest request, CancellationToken cancellationToken = default,
                      ClaimsPrincipal user = null, RequestQuota quota = null, object httpRequest = null) {
      if (_model == null)
        Initialize(); 
      var context = new RequestContext(this, request, cancellationToken, user, quota, httpRequest);
      return context; 
    }

    public async Task ExecuteRequestAsync(RequestContext context) {
      try {
        // validate 
        if (string.IsNullOrWhiteSpace(context.RawRequest.Query))
          throw new GraphQLException("Query may not be empty.");
        Events.OnRequestStarting(context);
        var handler = new RequestHandler(this, context);
        await handler.ExecuteAsync();
      } catch (AbortRequestException) {
        return; // error already added to response
      } catch (Exception ex) {
        context.AddError(ex); 
      } finally {
        context.Metrics.Duration = AppTime.GetDuration(context.StartTimestamp);
        if (context.Failed)
          Events.OnRequestError(context);
        Events.OnRequestCompleted(context); 
      }
    }

  }
}
