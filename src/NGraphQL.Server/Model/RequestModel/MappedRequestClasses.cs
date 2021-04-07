using System.Collections.Generic;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public SelectionItem Item;
    public List<RuntimeDirectiveBase> Directives;
    // we extract and keep include/skip directives separately for efficiency
    public List<PreparedSkipDirective> PreparedSkipDirectives;
    public MappedSelectionItem(SelectionItem item) { Item = item; }
    
    public override string ToString() => Item.ToString();

    public void AddDirective(RuntimeDirectiveBase dir) {
      Directives ??= new List<RuntimeDirectiveBase>();
      Directives.Add(dir);
      var skipAction = dir.Def.Handler as ISkipDirectiveAction;
      if (skipAction != null) {
        PreparedSkipDirectives ??= new List<PreparedSkipDirective>();
        PreparedSkipDirectives.Add(new PreparedSkipDirective() { Directive = dir, Action = skipAction });
      }
    }
    public bool HasDirectives => Directives != null;
    public bool HasSkipDirectives => PreparedSkipDirectives != null; 
  }

  public class PreparedSkipDirective {
    public RuntimeDirectiveBase Directive;
    public ISkipDirectiveAction Action; 
  }

  // Mapped field is runtime representation of a selection field in a query.
  // Mapped field set is a 'flattened' list of fields (with all fragments expanded)
  // for a specific output object type (when parent field returns union or interface)
  // It is also prepared for resolver invocation with mapped args.  
  public class MappedSelectionField: MappedSelectionItem {
    public readonly SelectionField Field;
    public readonly FieldDef FieldDef;
    public readonly IList<MappedArg> Args;

    public FieldResolverInfo Resolver;

    public MappedSelectionField(SelectionField field, FieldDef fieldDef, IList<MappedArg> args): base(field) {
      Field = field;
      FieldDef = fieldDef;
      Args = args; 
    }

    public override string ToString() => $"{Field.Key}";
    public static readonly IList<MappedSelectionField> EmptyList = new MappedSelectionField[] { };
  }

  public class MappedFragmentSpread: MappedSelectionItem {
    public readonly FragmentSpread Spread;
    public readonly IList<MappedSelectionItem> Items; 
    public MappedFragmentSpread(FragmentSpread spread, IList<MappedSelectionItem> items): base(spread) {
      Spread = spread;
      Items = items; 
    }
  }

  // used as MappedSelectionField args and request directive args
  public class MappedArg {
    public static readonly IList<MappedArg> EmptyList = new MappedArg[] { };

    public RequestObjectBase Anchor; 
    public InputValueDef ArgDef; 
    public InputValueEvaluator Evaluator;

    public MappedArg() { }
    public override string ToString() => $"{ArgDef.Name}/{ArgDef.TypeRef.Name}";
  }

  public class SelectionSubSetMapping {
    public ObjectTypeDef ObjectTypeDef;
    public IList<MappedSelectionItem> MappedItems = new List<MappedSelectionItem>();
    public OutputFieldSet StaticFieldSet; //when there's no @include/@skip directives
  }

  public class OutputFieldSet {
    public SelectionSubSetMapping SubSetMapping; 
    public IList<MappedSelectionField> Fields; 
  }

}
