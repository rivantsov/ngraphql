using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public readonly SelectionItemKind Kind;

    public MappedSelectionItem(SelectionItemKind kind) { 
      Kind = kind; 
    }
  }

  // Mapped field is a runtime representation of a selection field in a query.
  public class MappedSelectionField: MappedSelectionItem {
    public readonly SelectionField Field; 
    public readonly FieldResolverInfo Resolver;
    public readonly IList<MappedArg> MappedArgs;
    public int Index; 

    //public FieldResolverInfo Resolver;

    public MappedSelectionField(SelectionField field, FieldResolverInfo resolver, int index, IList<MappedArg> args): base( SelectionItemKind.Field) {
      Field = field; 
      Resolver = resolver; 
      MappedArgs = args;
    }

    public override string ToString() => $"{Field.Key}";
    public static readonly IList<MappedSelectionField> EmptyList = new MappedSelectionField[] { };
  }

  public class MappedFragmentSpread: MappedSelectionItem {
    public readonly FragmentSpread Spread;
    public MappedFragmentSpread(FragmentSpread spread): base(SelectionItemKind.FragmentSpread) {
      Spread = spread;
    }
  }

  public class MappedSelectionSubSet {
    public ObjectTypeMapping Mapping;
    public IList<MappedSelectionItem> MappedItems = new List<MappedSelectionItem>();
  }

}
