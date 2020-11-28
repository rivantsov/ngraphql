using System;
using System.Collections.Generic;

using NGraphQL.Model;
using NGraphQL.Server.Execution;

namespace NGraphQL.Server.RequestModel {
  // Mapped field is runtime representation of a selection field in a query.
  // Mapped field set is a 'flattened' list of fields (with all fragments expanded)
  // for a specific output object type (when parent field returns union or interface)
  // It is also prepared for resolver invocation with mapped args.  
  public class MappedField {
    public SelectionField SelectionField;
    public FieldDef FieldDef;
    public IList<MappedArg> Args;
    public IList<RequestDirective> IncludeSkipDirectives;

    public MappedField() { }

    public override string ToString() => $"{SelectionField.Key}";
    public static readonly IList<MappedField> EmptyList = new MappedField[] { };
  }

  // used as MappedField args and request directive args
  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor; 
    public InputValueDef ArgDef; 
    public IInputValueEvaluator Evaluator;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

  public interface IInputValueEvaluator {
    object GetValue(RequestContext context);
  }

  public class MappedObjectFieldSet {
    public ObjectTypeDef ObjectTypeDef;
    public IList<MappedField> Fields = new List<MappedField>();
  }

}
