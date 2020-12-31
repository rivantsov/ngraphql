using System.Collections.Generic;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public SelectionItem Item;
    public List<RuntimeDirectiveBase> Directives; 
    public MappedSelectionItem(SelectionItem item) { Item = item; }
    
    public override string ToString() => Item.ToString();

    public void AddDirective(RuntimeDirectiveBase dir) {
      Directives ??= new List<RuntimeDirectiveBase>();
      Directives.Add(dir); 
    }
    public bool HasDirectives => Directives != null && Directives.Count > 0; 
  }

  // Mapped field is runtime representation of a selection field in a query.
  // Mapped field set is a 'flattened' list of fields (with all fragments expanded)
  // for a specific output object type (when parent field returns union or interface)
  // It is also prepared for resolver invocation with mapped args.  
  public class MappedField: MappedSelectionItem {
    public readonly SelectionField Field;
    public readonly FieldDef FieldDef;
    public readonly IList<MappedArg> Args;

    public MappedField(SelectionField field, FieldDef fieldDef, IList<MappedArg> args): base(field) {
      Field = field;
      FieldDef = fieldDef;
      Args = args; 
    }

    public override string ToString() => $"{Field.Key}";
    public static readonly IList<MappedField> EmptyList = new MappedField[] { };
  }

  public class MappedFragmentSpread: MappedSelectionItem {
    public readonly FragmentSpread Spread;
    public readonly IList<MappedSelectionItem> Items; 
    public MappedFragmentSpread(FragmentSpread spread, IList<MappedSelectionItem> items): base(spread) {
      Spread = spread;
      Items = items; 
    }
  }

  // used as MappedField args and request directive args
  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor; 
    public InputValueDef ArgDef; 
    public InputValueEvaluator Evaluator;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

  public class MappedObjectItemSet {
    public ObjectTypeDef ObjectTypeDef;
    public IList<MappedSelectionItem> Items = new List<MappedSelectionItem>();
    public IList<MappedField> StaticMappedFields; //when there's no @include/@skip directives
  }

}
