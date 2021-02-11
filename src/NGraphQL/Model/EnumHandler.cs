﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public class EnumValueInfo : GraphQLModelObject {
    public FieldInfo Field; 
    public object Value;
    public long LongValue;
 }

  /// <summary>Handles conversions of enum values: to/from CLR enums vs stings and string arrays in GraphQL. </summary>
  public class EnumHandler {
    public static string[] EmptyStringArray = new string[] { };

    public string EnumName;
    public string Description;
    public Type Type;
    public Type UnderlyingType; 
    public bool IsFlagSet;
    public List<EnumValueInfo> Values = new List<EnumValueInfo>();
    public Dictionary<string, EnumValueInfo> ValuesLookup = 
      new Dictionary<string, EnumValueInfo>(StringComparer.OrdinalIgnoreCase);   //let's be forgiving about casing
    public Func<object, long> ConvertToLong;
    public object NoneValue;

    public EnumHandler(Type enumType, IList<ModelAdjustment> adjustments = null) {
      Type = enumType;
      UnderlyingType = Enum.GetUnderlyingType(enumType);
      var nameAttr = Type.GetAttribute<GraphQLNameAttribute>();
      EnumName = nameAttr?.Name ?? Type.Name;
      var descAttr = Type.GetAttribute<DescriptionAttribute>();
      Description = descAttr?.Description;
      IsFlagSet = enumType.HasAttribute<FlagsAttribute>();
      ConvertToLong = enumType.GetEnumToLongConverter();
      NoneValue = enumType.GetDefaultValue();
      // build enum value infos
      var values = Enum.GetValues(enumType);
      var fields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
      for (int i = 0; i < fields.Length; i++) {
        var fld = fields[i];
        if (fld.HasAttribute<IgnoreAttribute>() || HasAttribute<IgnoreAttribute>(fld, adjustments))
          continue;
        var value = values.GetValue(i);
        var fldLongValue = ConvertToLong(value);
        if (IsFlagSet && fldLongValue == 0) //Flags enum values with 0 value (like NONE) are ignored.
          continue;
        nameAttr = fld.GetAttribute<GraphQLNameAttribute>();
        var name = nameAttr?.Name ?? Utility.ToUnderscoreUpperCase(fld.Name);
        descAttr = fld.GetAttribute<DescriptionAttribute>();
        string descr = descAttr?.Description;
        var vInfo = new EnumValueInfo() {
          Field = fld,
          Value = value,
          Name = name,
          Description = descr,
          LongValue = fldLongValue
        };
        Values.Add(vInfo); 
        ValuesLookup.Add(vInfo.Name, vInfo);
      }
    }

    private bool ShouldIgnore(FieldInfo field, IList<ModelAdjustment> adjustments = null) {
      return HasAttribute<IgnoreAttribute>(field, adjustments);
    }
    
    private bool HasAttribute<TAttr>(FieldInfo field, IList<ModelAdjustment> adjustments = null) {
      if (adjustments == null || adjustments.Count == 0)
        return false;
      var attr = adjustments.FirstOrDefault(ad => ad.Type == this.Type && ad.MemberName == field.Name && ad.Attribute is TAttr);
      return attr != null; 
    }

    #region Value conversion methods ============================================================

    public string ConvertToSchemaDocString(object value) {
      if (value == null)
        return null;
      if (IsFlagSet) {
        var arr = ConvertFlagsEnumValueToOutputStringList(value) as IList<string>;
        var strValues = string.Join(", ", arr);
        return $"[{strValues}]";
      } else
        return ConvertEnumValueToOutputString(value);
    }

    public object ConvertStringToEnumValue (string outString) {
      if (string.IsNullOrEmpty(outString))
        return null;
      if (ValuesLookup.TryGetValue(outString, out var enumV))
        return enumV.Value;
      throw new Exception($"Invalid value {outString} for enum type {this.EnumName}");
    }

    public object ConvertStringListToFlagsEnumValue(IList<string> stringValues) {
      if (stringValues.Count == 0)
        return NoneValue;
      long result = 0;
      foreach (var sv in stringValues) {
        if (!ValuesLookup.TryGetValue(sv, out var valueInfo))
          throw new Exception($"Invalid value {sv} for enum type {this.EnumName}");
        result |= valueInfo.LongValue;
      }
      // convert long to typed enum value;
      var v = Enum.ToObject(this.Type, result);
      return v;
    }

    public IList<string> ConvertFlagsEnumValueToOutputStringList(object value) {
      // flags enums are represented as arrays of enum values (as strings)
      if (value == null)
        return null;
      var longV = this.ConvertToLong(value);
      if (longV == 0)
        return EmptyStringArray;
      var resultList = new List<string>();
      foreach (var enumV in this.Values) {
        if ((longV & enumV.LongValue) != 0)
          resultList.Add(enumV.Name);
      }
      return resultList.ToArray();
    }

    public string ConvertEnumValueToOutputString(object value) {
      try {
        var vinfo = Values.FirstOrDefault(vi => vi.Value.Equals(value));
        return vinfo?.Name;
      } catch (Exception) {
        return value.ToString();
      }
    }


    #endregion
  }
}