using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Introspection {

  public class IntrospectionResolvers {

    [Query("__schema", Hidden = true)]
    public Schema__ GetSchema(IFieldContext context) {
      return context.GetModel().Schema_;
    }

    [Query("__type", Hidden = true), Null]
    public Type__ GetType(IFieldContext context, string name) {
      var schema = context.GetModel().Schema_;
      var type = schema.Types.FirstOrDefault(t => t.Name == name);
      return type; 
    }

    [Field("fields", OnType = typeof(Type__)), Null]
    public IList<Field__> GetFields(IFieldContext context, Type__ type_, bool includeDeprecated = true) {
      return type_.Fields; 
    }

    [Field("enumValues", OnType = typeof(Type__)), Null]
    public IList<EnumValue__> GetEnumValues(IFieldContext context, Type__ type_, bool includeDeprecated = true) {
      return type_.Kind == TypeKind.Enum ? type_.EnumValues.ToArray() : null; 
    }

  }
}
