using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NGraphQL.Core;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    public string Name => GetType().Name;

    public readonly List<TypeRegistration> RegisteredTypes = new List<TypeRegistration>();
    public readonly List<EntityMapping> Mappings = new List<EntityMapping>();
    public readonly List<Type> ResolverClasses = new List<Type>();

    public readonly List<Scalar> Scalars = new List<Scalar>();
    public readonly List<Type> DirectiveTypes = new List<Type>();
    public readonly List<ModelAdjustment> Adjustments = new List<ModelAdjustment>();

    public GraphQLModule() {
    }

    protected void RegisterModelTypes(
        Type query = null,
        Type mutation = null,
        Type subscription = null,
        IList<Type> objectTypes = null,
        IList<Type> inputTypes = null, 
        IList<Type> enums = null, 
        IList<Type> interfaces = null, 
        IList<Type> unions = null
      ) {
      if (query != null)
        RegisterModelType(TypeRole.Query, query);
      if (mutation != null)
        RegisterModelType(TypeRole.Mutation, mutation);
      if (subscription != null)
        RegisterModelType(TypeRole.Subscription, subscription);
      RegisterTypes(TypeRole.Object, objectTypes);
      RegisterTypes(TypeRole.Input, inputTypes);
      RegisterTypes(TypeRole.Enum, enums);
      RegisterTypes(TypeRole.Interface, interfaces);
      RegisterTypes(TypeRole.Union, unions);
    }

    protected void RegisterResolvers(params Type[] resolverTypes) {
      ResolverClasses.AddRange(resolverTypes);
    }

    protected EntityMapping<TEntity> MapEntity<TEntity>() where TEntity : class {
      var mapping = new EntityMapping<TEntity>();
      Mappings.Add(mapping);
      return mapping;
    }

    protected void RegisterScalars(params Scalar[] scalars) {
      Scalars.AddRange(scalars);
    }

    protected void RegisterDirectives(params Type[] directiveTypes) {
      DirectiveTypes.AddRange(directiveTypes);
    }

    /// <summary>Adjusts model after construction. Use this method to modify GraphQL model for types that you
    /// use but do not have control of. For example, you can remove/hide a value from Enum.    /// 
    /// </summary>
    /// <param name="type">Adjustment type.</param>
    /// <param name="modelType">Model type</param>
    /// <param name="field">Optional, target field.</param>
    /// <param name="value">Optional, action argument.</param>
    public void AdjustModel(AdjustmentType type, Type modelType, string field = null, object value = null) {
      this.Adjustments.Add(new ModelAdjustment() { Type = type, ModelType = modelType, Field = field, Value = value });
    }

    protected internal T FromMap<T>(object value) {
      return default(T);
    }

    private void RegisterTypes(TypeRole role, IList<Type> types) {
      if (types == null)
        return;
      foreach (var type in types)
        RegisterModelType(role, type);
    }

    protected void RegisterModelType(TypeRole role, Type type, string graphQLName = null) {
      // TODO: validate type vs role
      this.RegisteredTypes.Add(new TypeRegistration() { Role = role, Type = type });
    }

  }
}
