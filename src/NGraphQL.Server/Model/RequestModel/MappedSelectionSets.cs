using System;
using System.Collections.Generic;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public abstract class MappedSelectionItem {
    public readonly SelectionItem Item; 

    public MappedSelectionItem(SelectionItem item) {
      Item = item; 
    }
    public override string ToString() => $"{Item.Name}";
  }

  // Mapped field is a runtime representation of a selection field in a query.
  public class MappedSelectionField: MappedSelectionItem {
    public readonly SelectionField Field;
    public readonly FieldResolverInfo Resolver;
    public readonly IList<MappedArg> MappedArgs;

    public MappedSelectionField(SelectionField field, FieldResolverInfo resolver, IList<MappedArg> mappedArgs) : base(field) {
      Field = field; 
      Resolver = resolver;
      MappedArgs = mappedArgs; 
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

    public override string ToString() => $"Mapping:{Mapping.EntityType}=>{Mapping.TypeDef}";
  }

}
