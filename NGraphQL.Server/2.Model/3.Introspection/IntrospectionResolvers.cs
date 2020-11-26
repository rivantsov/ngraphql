using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Introspection {

  public class IntrospectionResolvers {

    // Query resolvers
    public Schema__ GetSchema(IFieldContext context) {
      return context.GetModel().Schema_;
    }

    public Type__ GetType(IFieldContext context, string name) {
      var schema = context.GetModel().Schema_;
      var type = schema.Types.FirstOrDefault(t => t.Name == name);
      return type; 
    }

    //[Field("fields", OnType = typeof(Type__)), Null]
    public IList<Field__> GetFields(IFieldContext context, Type__ type_, bool includeDeprecated = true) {
      return type_.FieldList; 
    }

    //[Field("enumValues", OnType = typeof(Type__)), Null]
    public IList<EnumValue__> GetEnumValues(IFieldContext context, Type__ type_, bool includeDeprecated = true) {
      if (type_.Kind != TypeKind.Enum)
        return null;
      if (includeDeprecated)
        return type_.EnumValueList.ToArray();
      return type_.EnumValueList.Where(ev => !ev.IsDeprecated).ToArray();
    }

  }
}
