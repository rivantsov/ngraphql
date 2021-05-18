using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor;
    public InputValueDef ArgDef;
    public InputValueEvaluator Evaluator;
    public List<RuntimeDirective> Directives;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

  /*
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
    public readonly IList<MappedArg> MappedArgs;
    public int Index; 

    //public FieldResolverInfo Resolver;

    public MappedSelectionField(SelectionField field, FieldDef fieldDef, int index, IList<MappedArg> args): base(field) {
      FieldDef = fieldDef; 
      MappedArgs = args;
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
  public class MappedSelectionSubSet {
    public ObjectTypeDef ObjectTypeDef;
    public Type SourceType; 
    public IList<MappedSelectionItem> MappedItems = new List<MappedSelectionItem>();
  }

  */
  // used as MappedSelectionField args and request directive args
}
