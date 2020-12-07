using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Model {
  
  public interface IArgDirectiveAction {
    void PreviewArgValueSource(RequestContext context, InputValue argDef, ValueSource source);
    object CheckArgValue(RequestContext context, object value);
  }
  
  public interface IFieldDirectiveAction {
    void PreviewField(FieldContext context);
    object PreviewFieldResult(FieldContext context, object value);
  }
  public interface ISkipFieldDirectiveAction {
    bool SkipField(RequestContext context, MappedField field);
  }

  public static class DirectiveExtensions { 
    public static void ApplyPreviewArgValueSource(this IList<RuntimeDirective> directives, RequestContext context, InputValue argDef, ValueSource source) {
      directives.ApplyDirectives<IArgDirectiveAction>(d => d.PreviewArgValueSource(context, argDef, source));
    }

    public static void ApplyDirectives<T>(this IList<RuntimeDirective> directives, Action<T> action) where T: class {
      if (directives == null || directives.Count == 0)
        return;
      foreach (var dir in directives) {
        var dirT = dir as T;
        if (dirT != null)
          action(dirT); 
      }
    }

    public static object ApplyDirectives<T>(this IList<RuntimeDirective> directives, Func<T, object, object> func, object value) where T : class {
      if (directives == null || directives.Count == 0)
        return value;
      foreach (var dir in directives) {
        var dirT = dir as T;
        if (dirT != null)
          value = func(dirT, value);
      }
      return value; 
    }
  }
}
