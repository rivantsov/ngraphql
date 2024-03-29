﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.Model;
using NGraphQL.Introspection;

namespace NGraphQL.Model {
  public static partial class ModelExtensions {

    public static TypeDefBase GetTypeDef(this GraphQLApiModel model, Type clrType) {
      if(model.TypesByClrType.TryGetValue(clrType, out var typeDef))
        return typeDef;
      return null;
    }

    // TODO: implement dynamic mapping for Map scalar
    public static bool IsComplexReturnType(this TypeDefBase typeDef) {
      switch(typeDef.Kind) {
        case TypeKind.Object:
        case TypeKind.Interface:
        case TypeKind.Union:
        case TypeKind.InputObject:  // Input types can be returned as output
          return true;
        /*
        case TypeKind.Scalar:
          var sc = (ScalarTypeDef)typeDef;
          return sc.Scalar.CanHaveSelectionSubset;
        */
        default:
          return false;
      }   
    }

    internal static string GetTypeRefName(this TypeRef typeRef) {
      var baseName = typeRef.Inner == null ? typeRef.TypeDef.Name : typeRef.Inner.Name;
      switch(typeRef.Kind) {
        case TypeKind.List:
          return "[" + baseName + "]";
        case TypeKind.NonNull:
          return baseName + "!";
        default:
          return typeRef.TypeDef.Name;
      }
    }

    internal static bool Matches(this IList<TypeKind> typeKindList, IList<TypeKind> other) {
      if(typeKindList.Count != other.Count)
        return false;
      for(int i = 0; i < typeKindList.Count; i++)
        if(typeKindList[i] != other[i])
          return false;
      return true;
    }

    public static TypeRef FindTypeRef(this TypeDefBase typeDef, IList<TypeKind> kinds) {
      return typeDef.TypeRefs.FirstOrDefault(tr => tr.KindsPath.Matches(kinds));
    }

    internal static Type GetClrType(this TypeRef typeRef) {
      // Schema, Query, Mutation objects do not have ClrType
      if(typeRef.TypeDef.ClrType == null)
        return null; 
      Type baseType;
      Type resultType; 
      switch(typeRef.Kind) {
        case TypeKind.List:
          // special case: flags enums. arrays of string values are mapped into a single multi-flag value
          //  so we return ClrType of parent (element)
          var isFlagArray = (typeRef.TypeDef.IsEnumFlagArray() && typeRef.Rank == 1);
          if (isFlagArray) 
            return typeRef.Inner.ClrType;
          // regular case
          baseType = typeRef.Inner.ClrType;
          resultType = baseType.MakeArrayType();
          return resultType;

        case TypeKind.NonNull:
          baseType = typeRef.Inner.ClrType;
          // If it is Nullable<ValueType>, get the underlying type
          var underType = Nullable.GetUnderlyingType(baseType);
          resultType = underType ?? baseType;
          return resultType;
        
        default:
          resultType = typeRef.TypeDef.ClrType;
          if(resultType.IsValueType)
            resultType = typeof(Nullable<>).MakeGenericType(resultType);
          return resultType; 
      }
    }

    public static TypeRef GetCreateTypeRef(this TypeDefBase typeDef, IList<TypeKind> kinds) {
      var typeRef = typeDef.FindTypeRef(kinds);
      if(typeRef != null)
        return typeRef;
      // no existing match; create new one; first find/create its parent, recursively
      // remove last kind in list, but remember it
      var lastKind = kinds.Last();
      var parentKinds = kinds.Take(kinds.Count - 1).ToList();
      // At some point we must hit the exising TypeRef - either typeDef.typeRefNull or TypeRefNotNull
      var parent = typeDef.GetCreateTypeRef(parentKinds);
      typeRef = new TypeRef(parent, lastKind);
      typeDef.TypeRefs.Add(typeRef); 
      return typeRef; 
    }

    public static TypeRef GetListElementTypeRef(this TypeRef typeRef) {
      switch (typeRef.Kind) {
        case TypeKind.List: return typeRef.Inner;
        case TypeKind.NonNull: return GetListElementTypeRef(typeRef.Inner);
        default:
          if (typeRef.TypeDef.IsEnumFlagArray())
            return typeRef.TypeDef.TypeRefNotNull; //return enum type ref itself
          else
            return null;
      }
    }

  }
}
