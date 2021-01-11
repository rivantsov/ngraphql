using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Introspection {

  public class IntrospectionResolvers: IResolverClass {
    private __Schema _schema; 

    public void BeginRequest(IRequestContext request) {
      _schema = request.GetModel().Schema_;
    }

    public void EndRequest(IRequestContext request) {
    }

    // Query resolvers
    public __Schema GetSchema(IFieldContext context) {
      return _schema;
    }

    public __Type GetGraphQLType(IFieldContext context, string name) {
      var type = _schema.Types.FirstOrDefault(t => t.Name == name);
      return type; 
    }

    //[Field("fields", OnType = typeof(Type__)), Null]
    public IList<__Field> GetFields(IFieldContext context, __Type type_, bool includeDeprecated = true) {
      return type_.Fields.Where(t => !t.IsHidden).ToList(); 
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
