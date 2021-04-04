using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {

  public class ObjectTypeMapping {
    public Type GraphQLType;
    public Type EntityType;
    public LambdaExpression Expression; //might be null; in this case we have mapping using field name matches
    public ObjectTypeMapping() { }
  }

  public class ObjectTypeMapping<TEntity> : ObjectTypeMapping {
    internal ObjectTypeMapping() {
      base.EntityType = typeof(TEntity);
    }
    public void To<TGraphQL>(Expression<Func<TEntity, TGraphQL>> expression = null) where TGraphQL : class {
      GraphQLType = typeof(TGraphQL);
      Expression = expression;
    }
  }
}
