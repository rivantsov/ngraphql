﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public static class ConvertHelper {

    public static bool IsConvertibleFrom(this TypeRef target, TypeRef source) {
      if (source == target)
        return true;
      // cut-off not-null wrapper and check again
      if (target.Kind == TypeKind.NotNull)
        target = target.Parent;
      if (source.Kind == TypeKind.NotNull)
        source = source.Parent;
      if (source == target)
        return true; 

      // check arrays - must match rank and base type
      if (target.Rank > 0)
        return source.Rank == target.Rank && source.TypeDef == target.TypeDef;
      // by type kind
      switch(target.TypeDef) {
        case ScalarTypeDef scalar:
          return scalar.CanConvertFrom.Contains(source.TypeDef.ClrType);
        default:
          // all other cases - can convert only if exactly the same type.
          return target.TypeDef == source.TypeDef; 
      }
    }

    public static object ValidateConvert(this RequestContext context, object value, 
                                              TypeRef typeRef, RequestObjectBase anchor) {
      if (value == null) {
        if (typeRef.IsNotNull) 
          throw new InvalidInputException(
            $"Input value evaluated to null, but expected type '{typeRef.Name}' is not nullable.", anchor);
        return value;
      }
      // value not null; check if types match
      var valueType = value.GetType();
      if (valueType == typeRef.ClrType)
        return value;
      // check if typeRef is a nullable<t>; get base type and compare
      if (typeRef.TypeDef.ClrType == valueType)
        return value; 

      switch(typeRef.TypeDef) {
        case ScalarTypeDef scalar:
          // most common case - let Scalar take care of it
          var convValue = scalar.ConvertInputValue(value);
          return convValue;

        case EnumTypeDef etd:
          return etd.ConvertInputValue(context, value, anchor);

        case InputObjectTypeDef _:
          return value; 

        default:
          throw new Exception ($"Failed to convert value '{value}' to type {typeRef.Name}.");
      }
    }//method

  }
}
