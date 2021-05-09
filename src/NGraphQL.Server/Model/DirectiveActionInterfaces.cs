using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;

namespace NGraphQL.Model {

  public interface ISelectionItemDirectiveAction {
    void PreviewItem(SelectionItemContext context, object[] argValues);
    void BeforeResolve(SelectionItemContext context, object[] argValues);
    void AfterResolve(SelectionItemContext context, object[] argValues, ref object value);
  }

  public interface IInputValueDirectiveAction {
    void PreviewInputValueDef(GraphQLApiModel model, InputValueDef valueDef, object[] argValues);
    void PreviewVariable(RequestContext context, VariableDef varDef, object[] argValues);
    object ProcessValue(RequestContext context, object[] argValues, object value);
  }
  
  public interface IModelDirectiveAction {
    void Apply(GraphQLApiModel model, GraphQLModelObject element, object[] argValues);
  }
}
