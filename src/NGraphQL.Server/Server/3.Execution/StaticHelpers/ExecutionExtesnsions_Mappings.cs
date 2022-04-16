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

          /*
        case ScalarTypeDef std: 
          if (!std.Scalar.CanHaveSelectionSubset)
            throw new Exception($"FATAL: Failed to find type mapping. Scalar type {std.Scalar} may not be mapped from type {typeDef.Name}");
          return null;
        */
          // TODO: implement dynamic mapping for Map scalar

        default:
          // should never happen
          throw new Exception($"FATAL: Invalid target type kind {typeDef.Kind}, type {typeDef.Name}");
      }
      return mapping; 
    }

    public static ObjectTypeMapping FindObjectTypeMapping(this ObjectTypeDef typeDef, Type fromType) {
      var mapping = typeDef.Mappings.FirstOrDefault(m => m.EntityType == fromType 
              || m.EntityType.IsAssignableFrom(fromType));
      return mapping;
    }

    public static ObjectTypeMapping FindObjectTypeMapping(IList<ObjectTypeDef> typeDefs, Type fromType) {
      foreach(var typeDef in typeDefs) {
        var mapping = typeDef.Mappings.FirstOrDefault(m => m.EntityType == fromType
              || m.EntityType.IsAssignableFrom(fromType));
        if (mapping != null)
          return mapping;
      }
      return null; 
    }

  }
}
