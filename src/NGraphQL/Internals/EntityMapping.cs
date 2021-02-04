using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {

  public class EntityMapping {
    public Type GraphQLType;
    public Type EntityType;
    public LambdaExpression Expression; //might be null; in this case we have mapping using field name matches
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
