using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NGraphQL.Introspection;
using NGraphQL.CodeFirst.Internals;
using NGraphQL.Core;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    public string Name => GetType().Name;

    public readonly List<Type> ScalarTypes = new List<Type>();
    public readonly List<Type> EnumTypes = new List<Type>();
    public readonly List<Type> ObjectTypes = new List<Type>();
    public readonly List<Type> InputTypes = new List<Type>();
    public readonly List<Type> InterfaceTypes = new List<Type>();
    public readonly List<Type> UnionTypes = new List<Type>();
    public readonly List<DirectiveRegistration> RegisteredDirectives = new List<DirectiveRegistration>(); 
    public readonly List<ModelAdjustment> Adjustments = new List<ModelAdjustment>();
    public Type QueryType;
    public Type MutationType;
    public Type SubscriptionType;

    // Server-bound entities
    public readonly List<EntityMapping> Mappings = new List<EntityMapping>();
    public readonly List<Type> ResolverTypes = new List<Type>();

    public GraphQLModule() {
    }

    public EntityMapping<TEntity> MapEntity<TEntity>() where TEntity : class {
      var mapping = new EntityMapping<TEntity>();
      Mappings.Add(mapping);
      return mapping;
    }

    public T FromMap<T>(object value) {
      return default(T);
    }

  }
}
