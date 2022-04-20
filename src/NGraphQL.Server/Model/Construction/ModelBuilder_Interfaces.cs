using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
    }

    private void CollectNestedImplementedInterfacesRec(ComplexTypeDef typeDef) {
      typeDef.Processed = true; // do it upfront, to avoid endless loops
      if (typeDef.Implements.Count == 0 || typeDef.Processed)
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

    private void ValidateInterfaceImplementations() {
      var intfList = _model.GetTypeDefs<InterfaceTypeDef>(Introspection.TypeKind.Interface);
    }


    private IList<ComplexTypeDef> GetComplexTypes() {
      var list = new List<ComplexTypeDef>();
      foreach (var typeDef in _model.Types) {
        if (typeDef.ClrType == null) //exclude Intro objects and special types
          continue;
        if (typeDef is ComplexTypeDef complexType) // complex types - object types or interfaces - can implement interfaces
          list.Add(complexType);
      }
      return list;
    }

  }
}
