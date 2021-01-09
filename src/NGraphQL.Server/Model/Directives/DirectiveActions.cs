using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model {
  
  public interface IInputValueDirectiveAction {
    void PreviewInputValueDef(GraphQLApiModel model, InputValueDef valueDef, object[] argValues);
    void PreviewVariable(RequestContext context, VariableDef varDef, object[] argValues);
    object ProcessValue(RequestContext context, object[] argValues, object value);
  }
  
  public interface IFieldDirectiveAction {
    void PreviewField(FieldContext context);
    object PreviewFieldResult(FieldContext context, object value);
  }

  public interface ISkipDirectiveAction {
    bool ShouldSkip(RequestContext context, MappedSelectionItem item, object[] argValues);
  }

  public interface IModelDirectiveAction {
    void Apply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues);
  }
}
