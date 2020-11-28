using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Runtime;
using NGraphQL.Server.RequestModel;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public class RequestContext : IRequestContext {
    public GraphQLServer Server { get; }
    public ClaimsPrincipal User { get; set; } 
    public GraphQLApiModel ApiModel => Server.Model; 

    public GraphQLRequest RawRequest;
    public ParsedGraphQLRequest ParsedRequest;
    public GraphQLOperation Operation { get; internal set; }
    public IList<VariableValue> OperationVariables { get; } = new List<VariableValue>();
    public GraphQLResponse Response { get; } = new GraphQLResponse();
    public RequestMetrics Metrics { get; } = new RequestMetrics();
    public RequestQuota Quota; 
    /// <summary>Dictionary for use by resolvers to pass data around. </summary>
    public IDictionary<string, object> CustomData { get; } = new ConcurrentDictionary<string, object>();

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _externalCancellationToken;


    internal long StartTimestamp;
    // see below on CustomTypeRefs
    internal readonly IList<TypeRef> CustomTypeRefs = new List<TypeRef>();
    public IList<Exception> Exceptions = new List<Exception>();
    public bool Failed { get; internal set; }
    public readonly object Lock = new object();
    // used by http server
    public object GraphQLHttpRequest { get; } 

    public RequestContext(GraphQLServer server, GraphQLRequest rawRequest, CancellationToken cancellationToken = default, 
                          ClaimsPrincipal user = null, RequestQuota quota = null, object graphQLHttpRequest = null) {
      Server = server;
      RawRequest = rawRequest;
      _externalCancellationToken = cancellationToken;
      User = user;
      GraphQLHttpRequest = graphQLHttpRequest;
      StartTimestamp = AppTime.GetTimestamp();
      Quota = quota ?? Server.DefaultRequestQuota ?? new RequestQuota();
      // cancellation tokens, initial implementation of limiting request time by quota
      _cancellationTokenSource = new CancellationTokenSource(Quota.MaxRequestTime);
      _cancellationTokenSource.Token.Register(OnCancelled);
      if (_externalCancellationToken != default)
        _externalCancellationToken.Register(OnExternalTokenCancelled);
    }


    bool _externalCancelled;
    private void OnExternalTokenCancelled() {
      _externalCancelled = true; 
      _cancellationTokenSource.Cancel(); 
    }
    private void OnCancelled() {
      if (!_externalCancelled) {
        // we assume it's timeout, token is already signaling; add error
        var err = new GraphQLError(
          $"Request canceled, request time exceeded max specified by quota ({Quota.MaxOutputObjects}).",  type: "Quota");
        AddError(err);
      }
    }

    public void AddError(GraphQLError error, Exception sourceException = null) {
      lock (this.Lock) {
        this.Response.Errors.Add(error);
        if (sourceException != null)
          this.Exceptions.Add(sourceException); 
        this.Failed = true;
      }
    }
   
    public int GetQuotaTimeMs() {
      return 10000; 
    }
  }

    /* About custom type refs in RequestContext
      // when parsing the request, we try to lookup existing typeRef registered with TypeDef in the model; 
      // we might not find it; for ex - we are looking for type [[int]]!, but Model does not have any field or resolver arg
      // of this type. Model might have [[int]] type, and this is a legit case. We can create this new TypeRef,
      // but we cannot register it in typeDef's list - the model is read-only now.
      // So instead we add it to this request's typeRef list. 
    */

}
