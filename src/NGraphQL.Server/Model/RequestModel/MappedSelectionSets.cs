using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public readonly SelectionItem Item; 

    public MappedSelectionItem(SelectionItem item) {
      Item = item; 
    }
  }

  // Mapped field is a runtime representation of a selection field in a query.
  public class MappedSelectionField: MappedSelectionItem {
    public readonly SelectionField Field;
    public readonly FieldResolverInfo Resolver;
    public int Index; 

    //public FieldResolverInfo Resolver;

    public MappedSelectionField(SelectionField field, FieldResolverInfo resolver, int index) : base(field) {
      Field = field; 
      Resolver = resolver;
    }

    public override string ToString() => $"{Field.Key}";
    public static readonly IList<MappedSelectionField> EmptyList = new MappedSelectionField[] { };
  }

  public class MappedFragmentSpread: MappedSelectionItem {
    public readonly FragmentSpread Spread;
    public MappedFragmentSpread(FragmentSpread spread): base(spread) {
      Spread = spread;
    }
  }

  public class MappedSelectionSubSet {
    public ObjectTypeMapping Mapping;
    public IList<MappedSelectionItem> MappedItems = new List<MappedSelectionItem>();
  }

}
