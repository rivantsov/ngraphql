using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NGraphQL.Core;
using NGraphQL.Core.Scalars;

namespace NGraphQL.CodeFirst {

  public class GraphQLModule {
    public string Name => GetType().Name;

    public readonly GraphQLApi Api; 
    internal List<Type> Types = new List<Type>();
    internal List<EntityMapping> Mappings = new List<EntityMapping>();
    internal List<Type> ResolverClasses = new List<Type>();

    internal List<Scalar> Scalars = new List<Scalar>();
    internal List<Type> DirectiveTypes = new List<Type>();
    internal List<ModelAdjustment> Adjustments = new List<ModelAdjustment>();

    public GraphQLModule(GraphQLApi api) {
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

    public void RegisterScalars(params Scalar[] scalars) {
      Scalars.AddRange(scalars);
    }

    public void RegisterDirectives(params Type[] directiveTypes) {
      DirectiveTypes.AddRange(directiveTypes);
    }


    /// <summary>This method can be used to hide a value in pre-existing enum from its GraphQL equivalent/schema. 
    /// Use it module.OnModelConstructed method.</summary>
    /// <param name="enumValue"></param>
    public void IgnoreEnumValue(object enumValue) {
      this.Adjustments.Add(new ModelAdjustment() { Type = AdjustmentType.IgnoreMember, Target = enumValue });
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
