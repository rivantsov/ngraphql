﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    public string GetGraphQLNameFromAttribute(ICustomAttributeProvider metaObject) {
      var nameAttr = metaObject.GetCustomAttributes(typeof(GraphQLNameAttribute), inherit: true).FirstOrDefault() as GraphQLNameAttribute;
      if (nameAttr == null)
        return null; 
      if (string.IsNullOrWhiteSpace(nameAttr.Name)) {
        AddError($"GraphQLName may not be empty, type {metaObject}.");
        return null;
      }
      return nameAttr.Name;
    }

    public string GetGraphQLName(Type type) {
      var name = GetGraphQLNameFromAttribute(type);
      if (name != null)
        return name;
      name = type.Name;
      if (type.IsInterface && name.Length > 1 && name.StartsWith("I") && char.IsUpper(name[1]))
        name = name.Substring(1); //cut-off I
      // cut off _ suffix
      if (name.Length > 1 && name.EndsWith("_"))
        name = name.Substring(0, name.Length - 1);
      return name;
    }

    public string GetGraphQLName(MemberInfo member) {
      var name = GetGraphQLNameFromAttribute(member);
      if (name != null)
        return name;
      return member.Name.FirstLower();
    }

    public string GetGraphQLName(ParameterInfo param) {
      var name = GetGraphQLNameFromAttribute(param);
      if (name != null)
        return name;
      return param.Name.FirstLower();
    }

    public string GetEnumFieldGraphQLName(FieldInfo enumField) {
      var name = GetGraphQLNameFromAttribute(enumField);
      if (name != null)
        return name;
      return enumField.Name.ToUnderscoreCase().ToUpperInvariant();
    }

    public IList<Attribute> GetAllAttributesAndAdjustments(ICustomAttributeProvider provider, MethodBase paramOwner = null) {
      var attrs = provider.GetCustomAttributes(inherit: true).Select(a => a as Attribute).ToList();
      IList<Attribute> added = null; 
      switch(provider) {
        case Type t:
          added = _modelAdjustments.Where(a => a.Type == t && a.MemberName == null && a.ArgName == null)
                         .Select(a => a.Attribute).ToList();
          break;
        
        case MemberInfo m:
          added = _modelAdjustments.Where(a => a.Type == m.DeclaringType && a.MemberName == m.Name && a.ArgName == null)
                         .Select(a => a.Attribute).ToList();
          break;
        
        case ParameterInfo p:
          added = _modelAdjustments.Where(a => a.Type == paramOwner.DeclaringType && a.MemberName == paramOwner.Name 
                     && a.ArgName == p.Name).Select(a => a.Attribute).ToList();
          break; 
      }
      attrs.AddRange(added);
      return attrs; 
    }

    private void ReassignFieldIndexes(ObjectTypeDef typeDef) {
      for (int i = 0; i < typeDef.Fields.Count; i++)
        typeDef.Fields[i].Index = i;
    }

    public void AddError(string message) {
      _model.Errors.Add(message);
    }

  } //class
}
