using System.Collections.Generic;
using System.Linq;

using NGraphQL.Model;
using NGraphQL.Runtime;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Server.Execution {

  public static partial class ExecutionExtensions {

    public static bool IsSet(this GraphQLServerOptions options, GraphQLServerOptions option) {
      return (options & option) != 0;
    }

    public static void AbortIfFailed(this RequestContext context) {
      if (context.Failed)
        throw new AbortRequestException();
    }

    public static void AbortIfFailed(this FieldContext context) {
      if (context.RequestContext.Failed)
        throw new AbortRequestException();
    }


    public static IList<MappedField> GetIncludedMappedFields(this SelectionSubset subset, ObjectTypeDef typeDef, RequestContext context) {
      var outFieldSet = subset.MappedFieldSets.FirstOrDefault(fi => fi.ObjectTypeDef == typeDef);
      if (outFieldSet == null) {
        // this should never happen; but maybe add error here 
        return MappedField.EmptyList;
      }

      // Check include/skip directives
      var includedFields = outFieldSet.Fields.Where(f => ShouldInclude(f, context)).ToList();
      return includedFields;
    }

    private static bool ShouldInclude(MappedField field, RequestContext requestContext) {
      var dirs = field.Directives; 
      if(dirs == null || dirs.Count == 0)
        return true;
      foreach(var reqDir in dirs) {
        var dir = reqDir.CreateDirective(requestContext);
        var incDirDef = (ISkipDirectiveAction)dir;
        if(!incDirDef.ShouldSkip(requestContext, field))
          return false;
      }
      return true;
    }

    internal static IEnumerable<string> GetFieldNames(this MappedObjectFieldSet fieldSet) {
      return fieldSet.Fields.Select(mf => mf.FieldDef.Name);
    }

  }
}
