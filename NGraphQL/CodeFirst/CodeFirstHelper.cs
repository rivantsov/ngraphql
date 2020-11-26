using System;
using System.Linq;

namespace NGraphQL.CodeFirst {

  public static class CodeFirstHelper {

    /// <summary>This method can be used to hide a value in pre-existing enum from its GraphQL equivalent/schema. 
    /// Use it module.OnModelConstructed method.</summary>
    /// <param name="model"></param>
    /// <param name="enumValue"></param>
    public static void RemoveEnumValue(this GraphQLApiModel model, object enumValue) {
      var enumType = enumValue.GetType();
      var typeDef = model.GetTypeDef(enumType);
      var typeOk = typeDef != null && typeDef.Kind == TypeKind.Enum;
      if (!typeOk) {
        model.Errors.Add($"Removing enum value: type {enumType} is not registered or is not Enum.");
        return; 
      }
      var enumTypeDef = (EnumTypeDef)typeDef;
      var enumValueObj = enumTypeDef.EnumValues.FirstOrDefault(ev => ev.ClrValue.Equals(enumValue));
      if (enumValueObj == null) {
        model.Errors.Add($"Removing enum value: {enumValue} not found on type {typeDef.Name}");
        return;
      }
      enumTypeDef.EnumValues.Remove(enumValueObj);
    }


  }
}
