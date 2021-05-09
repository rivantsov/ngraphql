using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public SelectionItem Item;
    public List<RuntimeDirective> Directives;

    public MappedSelectionItem(SelectionItem item) { 
      Item = item; 
    }
    
    public override string ToString() => Item.ToString();

    public void AddDirective(RuntimeDirective dir) {
      Directives ??= new List<RuntimeDirective>();
      dir.Index = Directives.Count; 
      Directives.Add(dir);
    }
    public bool HasDirectives => Directives != null;
  }

  // Mapped field is a runtime representation of a selection field in a query.
  public class MappedSelectionField: MappedSelectionItem {
    public SelectionField Field => (SelectionField)base.Item; 
    public readonly FieldDef FieldDef;
    public readonly IList<MappedArg> Args;
    public int Index; 

    public FieldResolverInfo Resolver;

    public MappedSelectionField(SelectionField field, FieldDef fieldDef, IList<MappedArg> args): base(field) {
      FieldDef = fieldDef; 
      Args = args; 
    }

    public override string ToString() => $"{Field.Key}";
    public static readonly IList<MappedSelectionField> EmptyList = new MappedSelectionField[] { };
  }

  public class MappedFragmentSpread: MappedSelectionItem {
    public FragmentSpread Spread => (FragmentSpread) base.Item;
    public readonly IList<MappedSelectionItem> Items; 
    public MappedFragmentSpread(FragmentSpread spread, IList<MappedSelectionItem> items): base(spread) {
      Items = items; 
    }
  }

  // used as MappedSelectionField args and request directive args
  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor; 
    public InputValueDef ArgDef; 
    public InputValueEvaluator Evaluator;
    public List<RuntimeDirective> Directives;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

  public class SelectionSubSetMapping {
    public ObjectTypeDef ObjectTypeDef;
    public Type SourceType; 
    public IList<MappedSelectionItem> MappedItems = new List<MappedSelectionItem>();
    public OutputFieldSet StaticFieldSet; // when there's no @include/@skip directives
  }

  public class OutputFieldSet {
    public SelectionSubSetMapping SubSetMapping; 
    public IList<MappedSelectionField> Fields; 
  }

}
