using System;
using System.Collections.Generic;
using System.Text;

using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  public class DirectiveContext {
    public RequestContext RequestContext;
    public RuntimeDirective Directive;
    public IRuntimeDirectiveHandler Handler; 
    public object[] ArgValues;
    public object CustomData; 
  }

}
