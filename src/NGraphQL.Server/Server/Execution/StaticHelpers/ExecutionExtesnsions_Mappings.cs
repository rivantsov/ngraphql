using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  static partial class ExecutionExtensions {

    public static SelectionSubSetMapping GetMapping(this SelectionSubset subSet, Type fromType, TypeDefBase toTypeDef) {
      switch (toTypeDef) {
        case ObjectTypeDef otd:
          return subSet.GetMapping(fromType);
        case InterfaceTypeDef itd:
          return subSet.FindMapping(fromType, itd.PossibleTypes);
        case UnionTypeDef utd:
          return subSet.FindMapping(fromType, utd.PossibleTypes);
        default:
          // should never happen
          throw new Exception($"Invalid target type kind {toTypeDef.Kind}, type {toTypeDef.Name}");
      }
    }

    public static SelectionSubSetMapping FindMapping(this SelectionSubset subSet, Type fromType, IList<ObjectTypeDef> typeDefs) {
      foreach (var otd in typeDefs) {
        var m = subSet.GetMapping(fromType, otd);
        if (m != null)
          return m;
      }
      return null;
    }

    public static SelectionSubSetMapping GetMapping(this SelectionSubset subSet, Type fromType) {
      return subSet.Mappings.FirstOrDefault(m => m.SourceType == fromType);
    }


  }
}
