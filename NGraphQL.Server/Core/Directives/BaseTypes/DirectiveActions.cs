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

  public interface ISkipDirectiveAction {
    bool ShouldSkip(RequestContext context, MappedSelectionItem item);
  }

  public interface IModelDirectiveAction {
    void Apply(GraphQLApiModel model, GraphQLModelObject owner);
  }
}
