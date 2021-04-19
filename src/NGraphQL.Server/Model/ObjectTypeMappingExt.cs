using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;

namespace NGraphQL.Model {

  public class ObjectTypeMappingExt: ObjectTypeMapping {
    // accessed by field index
    public List<FieldResolverInfo> FieldResolvers = new List<FieldResolverInfo>();

    public ObjectTypeMappingExt(ObjectTypeMapping source) : this(source.EntityType, source.GraphQLType) {
      this.Expression = source.Expression;
    }

    public ObjectTypeMappingExt(Type selfMapType) : this(selfMapType, selfMapType) { }
    
    public ObjectTypeMappingExt(Type entityType, Type graphQLType) {
      this.EntityType = entityType;
      this.GraphQLType = graphQLType;
    }
  }

  public class FieldResolverInfo {
    public ObjectTypeMappingExt TypeMapping; 
    public FieldDef Field;
    public ResolverKind ResolverKind;
    public Func<object, object> ResolverFunc;
    public ResolverMethodInfo ResolverMethod;
    public Type OutType;
  }

}
