using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NGraphQL.Model;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    public readonly GraphQLApi Api; 
    internal List<Type> Types = new List<Type>();
    internal List<EntityMapping> Mappings = new List<EntityMapping>();
    internal List<Type> ResolverClasses = new List<Type>();

    internal List<ScalarTypeDef> Scalars = new List<ScalarTypeDef>();
    internal List<DirectiveDef> Directives = new List<DirectiveDef>();

    public GraphQLModule(GraphQLApi api) {
      Util.Check(api != null, "'api' parameter may not be null.");
      Api = api;
    }

    // called after model constructed
    public virtual void OnModelConstructed() {
    }

    public void RegisterTypes(params Type[] types) {
      Types.AddRange(types); 
    }

    public void RegisterResolvers(params Type[] resolverTypes) {
      ResolverClasses.AddRange(resolverTypes);
    }

    public void RegisterScalars(params ScalarTypeDef[] scalars) {
      Scalars.AddRange(scalars);
    }

    public void RegisterDirectives(params DirectiveDef[] directives) {
      Directives.AddRange(directives);
    }

    protected internal T FromMap<T>(object value) {
      return default(T);
    }

    // MapEntity - alt approach
    protected EntityMapping<TEntity> MapEntity<TEntity>() where TEntity: class {
      var mapping = new EntityMapping<TEntity>();
      Mappings.Add(mapping);
      return mapping; 
    }

  }
}
