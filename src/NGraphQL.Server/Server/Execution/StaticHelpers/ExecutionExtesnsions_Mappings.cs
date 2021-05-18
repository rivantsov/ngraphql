using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  static partial class ExecutionExtensions {

    public static ObjectTypeMapping FindMapping(this TypeDefBase typeDef, Type fromType) {
      ObjectTypeMapping mapping = null; 
      switch (typeDef) {
        case ObjectTypeDef otd:
          mapping = otd.FindObjectTypeMapping(fromType);
          break; 
        case InterfaceTypeDef itd:
          mapping = FindObjectTypeMapping(itd.PossibleTypes, fromType);
          break; 
        case UnionTypeDef utd:
          mapping = FindObjectTypeMapping(utd.PossibleTypes, fromType);
          break;
        default:
          // should never happen
          throw new Exception($"FATAL: Invalid target type kind {typeDef.Kind}, type {typeDef.Name}");
      }
      return mapping; 
    }

    public static ObjectTypeMapping FindObjectTypeMapping(this ObjectTypeDef typeDef, Type fromType) {
      var mapping = typeDef.Mappings.FirstOrDefault(m => m.EntityType == fromType);
      return mapping; 
    }

    public static ObjectTypeMapping FindObjectTypeMapping(IList<ObjectTypeDef> typeDefs, Type fromType) {
      foreach(var typeDef in typeDefs) {
        var mapping = typeDef.Mappings.FirstOrDefault(m => m.EntityType == fromType);
        if (mapping != null)
          return mapping;
      }
      return null; 
    }

    /*
    public static MappedSelectionSubSet GetMappedSubSet(this SelectionSubset subSet, Type fromType, TypeDefBase toTypeDef) {
      switch (toTypeDef) {
        case ObjectTypeDef otd:
          return subSet.GetMappedSubSet(fromType);
        case InterfaceTypeDef itd:
          return subSet.GetMappedSubSet(fromType, itd.PossibleTypes);
        case UnionTypeDef utd:
          return subSet.GetMappedSubSet(fromType, utd.PossibleTypes);
        default:
          // should never happen
          throw new Exception($"Invalid target type kind {toTypeDef.Kind}, type {toTypeDef.Name}");
      }
    }

    public static MappedSelectionSubSet GetMappedSubSet(this SelectionSubset subSet, Type fromType, IList<ObjectTypeDef> typeDefs) {
      foreach (var otd in typeDefs) {
        var m = subSet.GetMappedSubSet(fromType, otd);
        if (m != null)
          return m;
      }
      return null;
    }

    public static MappedSelectionSubSet GetMappedSubSet(this SelectionSubset subSet, Type fromType) {
      return subSet.MappedSubSets.FirstOrDefault(m => m.SourceType == fromType);
    }
    */

  }
}
