using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Model;

namespace NGraphQL.Model {

  public class ObjectTypeMappingExt: ObjectTypeMapping {
    // accessed by field index
    public List<ObjectFieldMapping> FieldMappings = new List<ObjectFieldMapping>();

    public ObjectTypeMappingExt(ObjectTypeMapping source) : this(source.EntityType, source.GraphQLType) {
      this.Expression = source.Expression;
    }

    public ObjectTypeMappingExt(Type selfMapType) : this(selfMapType, selfMapType) { }
    
    public ObjectTypeMappingExt(Type entityType, Type graphQLType) {
      this.EntityType = entityType;
      this.GraphQLType = graphQLType;
    }
  }

  public class ObjectFieldMapping {
    public ObjectTypeMappingExt TypeMapping; 
    public FieldDef Field;
    public ResolverMethodInfo ResolverInfo; 
    public Func<object, object> Reader;
    public FieldExecutionType ExecutionType;
  }

}
