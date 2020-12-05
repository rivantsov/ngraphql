using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model {

  public class DirectiveContext {
    public DirectiveMetadata Info; 
    public RequestContext RequestContext;
    public FieldContext FieldContext; 
  }
}
