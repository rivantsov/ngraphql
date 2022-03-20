using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using NGraphQL.Introspection;
using NGraphQL.Model;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    public string Name => GetType().Name;

    public readonly List<Type> ScalarTypes = new List<Type>();
    public readonly List<Type> EnumTypes = new List<Type>();
    public readonly List<Type> ObjectTypes = new List<Type>();
    public readonly List<Type> InputTypes = new List<Type>();
    public readonly List<Type> InterfaceTypes = new List<Type>();
    public readonly List<Type> UnionTypes = new List<Type>();
    public readonly List<DirectiveRegistration> Directives = new List<DirectiveRegistration>();
    public readonly List<DirectiveHandlerInfo> DirectiveHandlers = new List<DirectiveHandlerInfo>(); 
    public readonly List<ModelAdjustment> Adjustments = new List<ModelAdjustment>();
    public Type QueryType;
    public Type MutationType;
    public Type SubscriptionType;

    // Server-bound entities
    public readonly List<EntityMapping> EntityMappings = new List<EntityMapping>();
    public readonly List<Type> ResolverClasses = new List<Type>();

    public GraphQLModule() {
    }

    public EntityMapping<TEntity> MapEntity<TEntity>() where TEntity : class {
      var mapping = new EntityMapping<TEntity>();
      EntityMappings.Add(mapping);
      return mapping;
    }

    /// <summary>A fake method, placeholder. Use it in mapping expressions (MapEntity() method) to identify the automatic conversion
    /// from entity to GraphQL type based on registered type mappings. </summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>Default value for type. The method should never be invoked directly.</returns>
    public T FromMap<T>(object value) {
      return default;
    }

  }
}
