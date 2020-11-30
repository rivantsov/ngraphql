using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;

namespace NGraphQL.Introspection {

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
      return type_.Fields; 
    }

    //[Field("enumValues", OnType = typeof(Type__)), Null]
    public IList<__EnumValue> GetEnumValues(IFieldContext context, __Type type_, bool includeDeprecated = true) {
      if (type_.Kind != TypeKind.Enum)
        return null;
      if (includeDeprecated)
        return type_.EnumValues.ToArray();
      return type_.EnumValues.Where(ev => !ev.IsDeprecated).ToArray();
    }

  }
}
