using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NGraphQL.Introspection;
using NGraphQL.Server.Parsing;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  partial class ModelBuilder {

    private void LinkValidateInterfaces() {
      var complexTypes = GetComplexTypes();
      foreach (var complexType in complexTypes) {
        // try directly implemented interfaces
        var intTypes = complexType.ClrType.GetInterfaces();
        foreach (var iType in intTypes) {
          var iTypeDef = (InterfaceTypeDef)_model.GetTypeDef(iType);
          if (iTypeDef != null) {
            complexType.Implements.Add(iTypeDef);
            if (complexType is ObjectTypeDef objTypeDef)
              iTypeDef.PossibleTypes.Add(objTypeDef);
          }
        }
        // check ImplementsAttribute
        var implAttrs = complexType.ClrType.GetAttributes<ImplementsAttribute>();
        foreach (var implAttr in implAttrs)
          if(implAttr.Types != null && implAttr.Types.Length > 0) 
            foreach (var itype in implAttr.Types) { 
              if (!itype.IsInterface) {
                AddError($"ImplementsAttribute on type '{complexType}' refers to type '{itype}' which is not interface.");
                continue; 
              }
              var iTypeDef = (InterfaceTypeDef)_model.GetTypeDef(itype);
              if (iTypeDef == null) {
                AddError($"ImplementsAttribute on type '{complexType}' refers to interface '{itype}' which is not registered as a GraphQL interface.");
                continue;
              }
              complexType.Implements.Add(iTypeDef); 
            }// foreach implAttr

      } //foreach typeDef
      // collect all nested implemented interfaces
      foreach (var complexType in complexTypes) {
        CollectNestedImplementedInterfacesRec(complexType);
      }
      // collect Possible types for interfaces
      var objectTypes = _model.GetTypeDefs<ObjectTypeDef>(Introspection.TypeKind.Object);
      foreach (var objType in objectTypes)
        foreach (var intf in objType.Implements)
          intf.PossibleTypes.Add(objType);
      //validate interfaces
      foreach (var complexType in complexTypes) 
        foreach(var intfType in complexType.Implements)
          ValidateInterfaceImplementation(intfType, complexType);
      
    }

    private void CollectNestedImplementedInterfacesRec(ComplexTypeDef typeDef) {
      if (typeDef.Processed)
        return;
      typeDef.Processed = true; // do it upfront, to avoid endless loops
      if (typeDef.Implements.Count == 0)
        return;
      var allInterfaces = new HashSet<InterfaceTypeDef>(typeDef.Implements); 
      foreach(var intf in typeDef.Implements) {
        if (!intf.Processed)
          CollectNestedImplementedInterfacesRec(intf);
        allInterfaces.UnionWith(intf.Implements);
      }
      // check for circular refs
      if (typeDef is InterfaceTypeDef itypeDef && allInterfaces.Contains(itypeDef)) {
        AddError($"Detected circular 'Implements' reference for interface {itypeDef}");
        return; 
      }
      // replace with new set
      typeDef.Implements = allInterfaces.ToList();
    }

    private void ValidateInterfaceImplementation(InterfaceTypeDef intfType, ComplexTypeDef implType) {
      foreach (var intfField in intfType.Fields) {
        var implField = implType.FindField(intfField.Name);
        var fldName = $"{intfType.Name}.{intfField.Name}"; 
        if (implField == null) {
          AddError($"Interface field '{fldName}' is not implemented by type '{implType.Name}' implementing the interface.");
          continue; 
        }
        // check field type - impl type must be covariant to interface type
        if (!TypeIsCovariant(implField.TypeRef, intfField.TypeRef, fldName))
          continue; // errors should be posted
        // field arguments
        if (intfField.Args.Count != implField.Args.Count) {
          AddError($"Invalid implementation of interface field '{fldName}' in type '{implType.Name}': - arguments count mismatch.");
          continue;
        }
        foreach(var arg in intfField.Args) {
          var implArg = implField.Args.FirstOrDefault(a => a.Name == arg.Name);
          if (implArg == null) {
            AddError($"Invalid implementation of interface field '{fldName}' in type '{implType.Name}': - argument {arg.Name} not found.");
            continue; 
          }
          // in the future, we can make these covariant-
          if(implArg.TypeRef != arg.TypeRef) {
            AddError($"Invalid implementation of interface field '{fldName}' in type '{implType.Name}': " + 
              $" argument {arg.Name} type mismatch ({arg.TypeRef.Name} vs {implArg.TypeRef.Name}).");
            continue;
          }
        } //for each arg
      } //foreach field
    }

    private bool TypeIsCovariant(TypeRef covType, TypeRef type, string fullFieldName) {
      if (type == covType)
        return true;
      // 1. ArrayRank should be the same
      if (covType.Rank != type.Rank) {
        AddError($"Incompatible implementation of field '{fullFieldName}', type mismatch (array Rank).");
        return false; 
      }
      // 2. Check non-null
      if (type.Kind == TypeKind.NonNull && covType.Kind != TypeKind.NonNull) {
        AddError($"Incompatible implementation of field '{fullFieldName}', implementing field return type must be non-null.");
        return false;
      }
      // 3. Get typeDefs
      var covTypeDef = covType.TypeDef;
      var typeDef = type.TypeDef;
      if (covTypeDef == typeDef)
        return true;
      // 4. if type is interface, then implementor must be interface or Object type
      if (typeDef is InterfaceTypeDef iTypeDef) {
        if (!(covTypeDef is ComplexTypeDef covComplexTypeDef)) {
          AddError($"Incompatible implementation of field '{fullFieldName}', return type mismatch.");
          return false;
        }
        if (!covComplexTypeDef.Implements.Contains(iTypeDef)) {
          AddError($"Incompatible implementation of field '{fullFieldName}', return type {covTypeDef.Name} does not implement interface '{iTypeDef.Name}'.");
          return false;
        }
        return true; 
      } // if type is interface

      AddError($"Incompatible implementation of field '{fullFieldName}', return type mismatch; ({covType.Name} vs {type.Name}).");
      return false; 
    }

    private IList<ComplexTypeDef> GetComplexTypes() {
      var list = new List<ComplexTypeDef>();
      foreach (var typeDef in _model.Types) {
        if (typeDef.ClrType == null || typeDef.Name.StartsWith("__")) //exclude Intro objects and special types
          continue;
        if (typeDef is ComplexTypeDef complexType) // complex types - object types or interfaces - can implement interfaces
          list.Add(complexType);
      }
      return list;
    }

  }
}
