using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace NGraphQL.CodeFirst {

  public class EntityMapping {
    public Type GraphQLType;
    public Type EntityType;
    public LambdaExpression Expression;
    public EntityMapping() { }
  }

  public class EntityMapping<TEntity> : EntityMapping {
    internal EntityMapping() {
      base.EntityType = typeof(TEntity);
    }
    public void To<TGraphQL>(Expression<Func<TEntity, TGraphQL>> expression = null) where TGraphQL : class {
      GraphQLType = typeof(TGraphQL);
      Expression = expression;
    }
    public void ToUnion<TUnion>() where TUnion : UnionBase {
      GraphQLType = typeof(TUnion);
    }
  }
}
