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

    public void Map<TEntity, TGraphQL>(Expression<Func<TEntity, TGraphQL>> mapping) {
      RegisterTypes(typeof(TGraphQL));
      var tmap = new EntityMapping() { GraphQLType = typeof(TGraphQL), EntityType = typeof(TEntity), Expression = mapping };
      Mappings.Add(tmap);
    }

    // defines default mapping, fields with the same name are mapped
    public void Map<TEntity, TGraphQL>() {
      RegisterTypes(typeof(TGraphQL));
      var tmap = new EntityMapping() { GraphQLType = typeof(TGraphQL), EntityType = typeof(TEntity) };
      Mappings.Add(tmap);
    }

    protected internal T FromMap<T>(object value) {
      return default(T);
    }

  }
}
