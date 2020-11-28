using System;

namespace NGraphQL.Runtime {

  /// <summary>Defines values for error type. The error type is reported 
  /// in the error.Extensions dictionary under the key "type". </summary>
  public static class ErrorTypes {
    public const string BadRequest = "BAD_REQUEST";
    public const string Syntax = "SYNTAX_ERROR";
    public const string InputError = "INPUT_ERROR";
    public const string ObjectNotFound = "OBJECT_NOT_FOUND";
    public const string Cancelled = "CANCELLED";
    public const string ResolverError = "RESOLVER_ERROR";
    public const string ServerError = "SERVER_ERROR"; // fatal server failure, unexpected
  }

}
