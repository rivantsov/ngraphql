using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;
using NGraphQL.Utilities;

namespace NGraphQL.Model {

  public class EnumValue : GraphQLModelObject {
    public object ClrValue;
    public string ClrName; 
    public long LongValue;  // enum value converted to int
    public IList<Directive> Directives;
  }


  public class EnumTypeDef : TypeDefBase {
    public static string[] EmptyStringArray = new string[] { };

    public List<EnumValue> EnumValues = new List<EnumValue>();
    public bool IsFlagSet;
    public readonly object NoneValue;
    public readonly Type EnumBaseType;
    public Func<object, long> ToLong; 

    public EnumTypeDef(string name, Type enumType, bool isFlagSet) : base(name, TypeKind.Enum, enumType) {
      base.ClrType = enumType;
      IsFlagSet = isFlagSet; 
      EnumBaseType = Enum.GetUnderlyingType(enumType);
      NoneValue = Activator.CreateInstance(enumType);
      ToLong = ReflectionHelper.GetEnumToLongConverter(enumType); 
    }

    public override object ToOutput(FieldContext context, object value) {
      if(value == null)
        return null;
      if(IsFlagSet)
        return FlagsEnumValueToOutput(value);
      else
        return EnumValueToOutput(value);
    }

    public object ConvertInputValue(RequestContext context, object inpValue, RequestObjectBase anchor) {
      if (inpValue == null)
        return null;
      if (IsFlagSet) {
        if (inpValue is string s)
          inpValue = new string[] { s };
        if (inpValue is IList<string> strings)
          return FlagsValueFromOutputStrings(strings);
        throw new InvalidInputException($"Input value '{inpValue}' cannot be converted to type '{this.Name}'; expected list of strings.",
          anchor);
      } else {
        // not input flags
        if (!(inpValue is string))
          throw new InvalidInputException($"Input value '{inpValue}' cannot be converted to type '{this.Name}'; expected string", anchor);
        return this.EnumValueFromOutput((string)inpValue);
      } //else 
    }

    public object FlagsValueFromOutputStrings(IList<string> strings) {
      if(strings.Count == 0)
        return NoneValue;
      long result = 0; 
      foreach(var s in strings) {
        var enumVal = EnumValues.FirstOrDefault(ev => ev.Name == s);
        if(enumVal == null)
          throw new Exception($"Invalid value {s} for enum type {this.Name}");
        result |= enumVal.LongValue; 
      }
      // convert long to enum;
      var v = Enum.ToObject(this.ClrType, result); 
      return v; 
    }

    public object EnumValueFromOutput(string outString) {
      var enumV = this.EnumValues.FirstOrDefault(ev => ev.Name == outString);
      return enumV?.ClrValue;
    }

    public override string FormatConstant(object value) {
      if(value == null)
        return null;
      if(IsFlagSet) {
        var arr = FlagsEnumValueToOutput(value) as IList<string>;
        var strValues = string.Join(", ", arr);
        return $"[{strValues}]";
      } else
        return EnumValueToOutput(value).ToString();
    }

    public object EnumValueToOutput(object value) {
      var longV = Convert.ToInt64(value);
      try {
        // TODO: implement smth more efficient, probably dict or array
        var member = this.EnumValues.FirstOrDefault(m => m.LongValue == longV);
        return member.Name;
      } catch(Exception) {
        return value.ToString();
      }
    }

    private object FlagsEnumValueToOutput(object value) {
      // flags enums are represented as arrays of enum values (as strings)
      if(value == null)
        return null;
      var longV = ToLong(value);
      if (longV == 0)
        return EmptyStringArray;
      var resultList = new List<string>(); 
      foreach(var enumV in this.EnumValues ) {
        if((longV & enumV.LongValue) != 0)
          resultList.Add(enumV.Name);
      }
      return resultList.ToArray(); 
    }

    public object CombineFlags(IList<object> flags) {
      long result = 0;
      for(int i = 0; i < flags.Count; i++)
        result |= ToLong(flags[i]);
      return Enum.ToObject(this.ClrType, result); 
    }

  } //class
}
