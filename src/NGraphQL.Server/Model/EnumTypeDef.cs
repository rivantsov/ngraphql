using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;
using System.Collections;
using NGraphQL.Internals;

namespace NGraphQL.Model {

  public class EnumTypeDef : TypeDefBase {
    public EnumHandler Handler; 

    public EnumTypeDef(Type type, IList<Attribute> attrs, GraphQLModule module) 
          : base(type.Name, TypeKind.Enum, type, attrs, module) {
      Handler = new EnumHandler(type);
      base.Name = Handler.EnumName;      
    }

    public override object ToOutput(FieldContext context, object value) {
      return ToOutputRec(context, context.FieldDef.TypeRef, value); 
    }

    public override string ToSchemaDocString(object value) {
      return Handler.ConvertToSchemaDocString(value); 
    }

    // Recursive method, for high-rank arrays
    private object ToOutputRec(FieldContext context, TypeRef typeRef, object value) {
      if (value == null)
        return null;
      if (typeRef.Kind == TypeKind.NonNull)
        typeRef = typeRef.Inner;
      if (Handler.IsFlagSet && typeRef.Rank == 1)
        return Handler.ConvertFlagsEnumValueToStringList(value);
      if (typeRef.IsList)
        return ArrayToOutputRec(context, typeRef, value);
      return Handler.ConvertEnumValueToOutputString(value);
    }

    private object ArrayToOutputRec(FieldContext context, TypeRef typeRef, object value) {
      if (value == null)
        return null;
      var list = value as IList;
      var result = new object[list.Count];
      var elemTypeRef = typeRef.Inner;
      for (int i = 0; i < result.Length; i++)
        result[i] = ToOutputRec(context, elemTypeRef, list[i]);
      return result; 
    }

    public object ConvertInputEnumValue(RequestContext context, object inpValue, RequestObjectBase anchor) {
      if (inpValue == null)
        return null;
      if (Handler.IsFlagSet) {
        if (inpValue is string s)
          inpValue = new string[] { s };
        if (inpValue is IList<string> strings)
          return Handler.ConvertStringListToFlagsEnumValue(strings);
        throw new InvalidInputException(
          $"Input value '{inpValue}' cannot be converted to type '{this.Name}'; expected list of strings.", anchor);
      } else {
        // not input flags
        if (!(inpValue is string stringValue))
          throw new InvalidInputException($"Input value '{inpValue}' cannot be converted to type '{this.Name}'; expected string", anchor);
        return Handler.ConvertStringToEnumValue(stringValue);
      } //else 
    }

    public object ConvertFlagListToEnumValue(IList<object> flags) {
      long result = 0;
      for(int i = 0; i < flags.Count; i++)
        result |= Handler.ConvertToLong(flags[i]);
      return Enum.ToObject(this.ClrType, result); 
    }

  } //class
}
