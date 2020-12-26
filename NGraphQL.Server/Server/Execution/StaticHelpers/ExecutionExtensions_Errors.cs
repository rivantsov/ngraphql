﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.RequestModel;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {
  public static partial class ExecutionExtensions {

    public static GraphQLError AddError(this RequestContext requestContext, Exception exc,
                                               IList<object> path = null, QueryLocation location = null) {
      var err = new GraphQLError(exc.Message, path, location, ErrorCodes.ServerError);
      var withDet = requestContext.Server.Settings.Options.IsSet(GraphQLServerOptions.ReturnExceptionDetails);
      if (withDet)
        err.Extensions["Details"] = exc.ToText();
      requestContext.AddError(err, exc);
      return err;
    }

    public static void AddError(this FieldContext fieldContext, Exception exc, string errorType) {
      var reqCtx = (RequestContext) fieldContext.RequestContext;
      var path = fieldContext.GetFullRequestPath();
      var err = new GraphQLError(exc.Message, path, fieldContext.SelectionField.Location, type: errorType);
      var withDet = reqCtx.Server.Settings.Options.IsSet(GraphQLServerOptions.ReturnExceptionDetails);
      if (withDet)
        err.Extensions["Details"] = exc.ToText();
      reqCtx.AddError(err, exc);
    }


    public static void AddInputError (this IRequestContext context, InvalidInputException exc) {
      var path = exc.Anchor.GetRequestObjectPath();
      var loc = exc.Anchor.Location; 
      var err = new GraphQLError(exc.Message, path, loc, ErrorCodes.InputError);
      context.AddError(err);
    }

    public static void AddInputError(this RequestContext context, string message, RequestObjectBase anchor) {
      var path = anchor.GetRequestObjectPath();
      var loc = anchor.Location;
      var err = new GraphQLError(message, path, loc, ErrorCodes.InputError);
      context.AddError(err);
    }

    public static string ToCommaText(this IList<object> path) {
      if (path == null)
        return string.Empty;
      return string.Join(",", path);
    }

    internal static IList<object> GetRequestObjectPath(this RequestObjectBase obj) {
      var path = (obj.Parent == null) ? new List<object>() : obj.Parent.GetRequestObjectPath();
      if (obj is NamedRequestObject namedObj && !(obj is GraphQLOperation)) // Operation name is NOT included in path
        path.Add(namedObj.Name);
      return path;
    }

    internal static void ThrowFieldDepthExceededQuota(this FieldContext fieldContext) {
      var reqCtx = (RequestContext) fieldContext.RequestContext;
      var quota = reqCtx.Quota;
      var err = new GraphQLError($"Query depth exceeded maximum ({quota.MaxDepth}) allowed by quota.",
        fieldContext.GetFullRequestPath(), fieldContext.SelectionField.Location, type: "Quota");
      reqCtx.AddError(err);
      throw new AbortRequestException();
    }

    internal static void ThrowObjectCountExceededQuota(this FieldContext fieldContext) {
      var reqCtx = (RequestContext)fieldContext.RequestContext;
      var quota = reqCtx.Quota;
      var err = new GraphQLError($"Output object count exceeded maximum ({quota.MaxOutputObjects}) allowed by quota.",
        fieldContext.GetFullRequestPath(), fieldContext.SelectionField.Location, type: "Quota");
      reqCtx.AddError(err);
      throw new AbortRequestException();
    }

    internal static void ThrowRequestCancelled(this FieldContext fieldContext) {
      var reqCtx = (RequestContext)fieldContext.RequestContext;
      var err = new GraphQLError($"Request cancelled",
        fieldContext.GetFullRequestPath(), fieldContext.SelectionField.Location, type: "Cancel");
      reqCtx.AddError(err);
      throw new AbortRequestException();
    }


  }
}
