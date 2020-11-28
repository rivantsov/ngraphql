using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Irony.Ast;
using NGraphQL.Model.Introspection;
using NGraphQL.Server;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Construction;
using NGraphQL.Core.Introspection;

namespace NGraphQL.Model {

  public class GraphQLApiModel {
    public readonly GraphQLServer Server;

    public ObjectTypeDef QueryType;
    public ObjectTypeDef MutationType;
    public ObjectTypeDef SubscriptionType;
    public ObjectTypeDef Schema;

    // Introspection schema object
    public __Schema Schema_;
    public string SchemaDoc { get; internal set; }

    public Dictionary<string, DirectiveDef> Directives = new Dictionary<string, DirectiveDef>();
    public IList<ResolverClassInfo> Resolvers = new List<ResolverClassInfo>(); 
    public List<TypeDefBase> Types = new List<TypeDefBase>();
    public Dictionary<string, TypeDefBase> TypesByName =
          new Dictionary<string, TypeDefBase>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Type, TypeDefBase> TypesByClrType = new Dictionary<Type, TypeDefBase>();
    public Dictionary<Type, ComplexTypeDef> TypesByEntityType = new Dictionary<Type, ComplexTypeDef>();

    public IList<string> Errors = new List<string>();
    public bool HasErrors => Errors.Count > 0;


    public GraphQLApiModel(GraphQLServer server) {
      Server = server; 
    }

  }


}
