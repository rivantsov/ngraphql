﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NGraphQL.Model;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    internal HashSet<Type> RegisteredTypes = new HashSet<Type>();
    internal List<EntityMapping> Mappings = new List<EntityMapping>();
    internal List<ResolverClassInfo> ResolverClasses = new List<ResolverClassInfo>(); 

    // called after model constructed
    public virtual void OnModelConstructed(GraphQLApi api) {
    }

    public void RegisterTypes(params Type[] types) {
      RegisteredTypes.UnionWith(types); 
    }

    public void RegisterResolversClass<T>() {
      RegisterResolversClass<T, T>(); 
    }
    
    public void RegisterResolversClass<TDecl, TImpl>() {
      var ci = new ResolverClassInfo() { Declaration = typeof(TDecl), Implementation = typeof(TImpl) };
      ResolverClasses.Add(ci); 
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
