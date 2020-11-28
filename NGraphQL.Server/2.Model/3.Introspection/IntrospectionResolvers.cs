using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Introspection {

  public class IntrospectionResolvers {

    // Query resolvers
    public __Schema GetSchema(IFieldContext context) {
      return context.GetModel().Schema_;
    }

    public __Type GetType(IFieldContext context, string name) {
      var schema = context.GetModel().Schema_;
      var type = schema.Types.FirstOrDefault(t => t.Name == name);
      return type; 
    }

    //[Field("fields", OnType = typeof(Type__)), Null]
    public IList<__Field> GetFields(IFieldContext context, __Type type_, bool includeDeprecated = true) {
      return type_.FieldList; 
    }

    //[Field("enumValues", OnType = typeof(Type__)), Null]
    public IList<__EnumValue> GetEnumValues(IFieldContext context, __Type type_, bool includeDeprecated = true) {
      if (type_.Kind != __TypeKind.Enum)
        return null;
      if (includeDeprecated)
        return type_.EnumValueList.ToArray();
      return type_.EnumValueList.Where(ev => !ev.IsDeprecated).ToArray();
    }

  }
}
