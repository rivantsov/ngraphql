using System;
using System.Collections.Generic;
using NGraphQL.Introspection;
using NGraphQL.Server;

namespace NGraphQL.Model {

  public class GraphQLApiModel {

    public ObjectTypeDef QueryType;
    public ObjectTypeDef MutationType;
    public ObjectTypeDef SubscriptionType;
    public ObjectTypeDef Schema;
    public __Schema Schema_;  // Introspection
    public string SchemaDoc { get; internal set; }

    // All types and various lookups
    public List<TypeDefBase> Types = new List<TypeDefBase>();
    public Dictionary<string, TypeDefBase> TypesByName = new Dictionary<string, TypeDefBase>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<Type, TypeDefBase> TypesByClrType = new Dictionary<Type, TypeDefBase>();
    public Dictionary<Type, IList<ObjectTypeMapping>> EntityMappings = new Dictionary<Type, IList<ObjectTypeMapping>>();

    public IList<ResolverClassInfo> ResolverClasses = new List<ResolverClassInfo>();
    public Dictionary<string, DirectiveDef> Directives = new Dictionary<string, DirectiveDef>();

    public IList<string> Errors = new List<string>();
    public bool HasErrors => Errors.Count > 0;
  }


}
