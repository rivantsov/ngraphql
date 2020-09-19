using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Irony.Ast;
using NGraphQL.Model.Introspection;
using NGraphQL.Server;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model {

  public class GraphQLApiModel {
    public readonly GraphQLApi Api; 

    public ObjectTypeDef QueryType;
    public ObjectTypeDef MutationType;
    public ObjectTypeDef SubscriptionType;
    public ObjectTypeDef Schema;
    public Dictionary<string, DirectiveDef> Directives;

    // Introspection schema object
    public Schema__ Schema_;
    public string SchemaDoc { get; internal set; }

    public List<TypeDefBase> Types = new List<TypeDefBase>();
    public Dictionary<string, TypeDefBase> TypesByName =
          new Dictionary<string, TypeDefBase>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Type, TypeDefBase> TypesByClrType = new Dictionary<Type, TypeDefBase>();
    public Dictionary<Type, EntityMapping> EntityMappings = new Dictionary<Type, EntityMapping>();

    public IList<string> Errors = new List<string>();
    public bool Faulted => Errors.Count > 0;


    public GraphQLApiModel(GraphQLApi api) {
      Api = api;
      Api.Model = this; 
    }

    public bool HasErrors => Errors.Count > 0;  
  }


}
