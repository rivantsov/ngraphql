using System; 
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.CodeFirst {

  public static class ErrorExtensions {

    public static GraphQLError AddError(this IFieldContext fieldContext, string message,
                                             string type = ErrorTypes.InputError) {
      var loc = fieldContext.SelectionField.Location;
      var path = fieldContext.GetFullRequestPath();
      var err = new GraphQLError(message, path, loc, type);
      fieldContext.RootField.AddError(err);
      return err;
    }

    public static GraphQLError AddErrorIf(this IFieldContext fieldContext, bool condition, string message, 
                                              string type = ErrorTypes.InputError) {
      if (!condition)
        return null;
      return AddError(fieldContext, message, type); 
    }

    public static void AbortIf(this IFieldContext fieldContext, bool condition, string message, 
                                string type = ErrorTypes.InputError) {
      if (condition) {
        AddError(fieldContext, message, type);
        throw new AbortRequestException();
      }
    }

    public static void AbortIfErrors (this IFieldContext context) {
      if (!context.RootField.Failed)
        return; 
      throw new AbortRequestException();
    }

  }
}
