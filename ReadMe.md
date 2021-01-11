# NGraphQL - GraphQL for .NET 

**NGraphQL** is a set of packages implementing [GraphQL APIs](https://spec.graphql.org/) client and server components in .NET.  

## Installation
Install the latest stable binaries via [NuGet](https://www.nuget.org/packages/NGraphQL/).
```
> dotnet add package NGraphQL
```

*NGrapQL* package is a slim library with types and definitions used both by Client and Server components. Here is the full list of nuget packages: 

|Package|Description|
|-------|-----------|
|NGraphQL|Basic classes shared by client and server components.|
|NGraphQL.Client|GraphQL client.|
|NGraphQL.Server|GraphQL server implementation not tied to a specific transport protocol.|
|NGraphQL.Server.AspNetCore|GraphQL HTTP server based on ASP.NET Core stack.|

## Basic Usage

### Server
Create a .NET Standard class library project and add references to *NGraphQL*, *NGraphQL.Server* packages. Start defining your GraphQL API model - types, fields/methods etc. GraphQL types are defined as POCO classes/interfaces decorated with attributes: 

```csharp
  /// <summary>A starship. </summary>
  public class Starship_ {
    /// <summary>The ID of the starship </summary>
    [Scalar("ID")]
    public string Id { get; set; }
    /// <summary>The name of the starship </summary>
    public string Name { get; set; }
    /// <summary>Length of the starship, along the longest axis </summary>
    [GraphQLName("length")]
    public float? GetLength(LengthUnit unit = LengthUnit.Meter) { return default; }
  }
``` 

We use the underscore suffix in type names to avoid name collisions with the underlying 'business' entities. This prefix will be automatically stripped by the engine in Schema definition. The XML comments will appear in the Schema document as GraphQL descriptions. The \[Null\] attribute marks the field as nullable; everything is non-nullable by default, except *Nullable\<T\>* types like *int?*. 

The top-level Query, Mutation types are defined as an interface:      
```csharp
  interface IStarWarsQuery {
    [Resolver("GetStarships")]
    IList<Starship_> Starships { get; }
    [GraphQLName("starship"), Resolver("GetStarshipAsync")]
    Starship_ GetStarship([Scalar("ID")] string id);
  }
``` 

Once you defined all types, interfaces,  unions etc, you register them as part of a GraphQL module: 

```csharp
  public class StarWarsApiModule: GraphQLModule {
    public StarWarsApiModule() {
      this.EnumTypes.AddRange(new Type[] { typeof(Episode), typeof(LengthUnit), typeof(Emojis) });
      this.ObjectTypes.AddRange(new Type[] 
      { typeof(Human_), typeof(Droid_), typeof(Starship_), typeof(Review_) });
      this.InterfaceTypes.Add(typeof(ICharacter_));
      this.UnionTypes.Add(typeof(SearchResult_));
      this.InputTypes.Add(typeof(ReviewInput_));
      this.QueryType = typeof(IStarWarsQuery);
      this.MutationType = typeof(IStarWarsMutation);
      this.ResolverTypes.Add(typeof(StarWarsResolvers));

      // (skipped) map app entity types to GraphQL Api types
    } 
  }
``` 

> Note that you can use .NET Enum types as-is. The engine translates the pascal-cased value *ValueOne* in c# definition into a GraphQL-style string *VALUE_ONE*, and all conversions at runtime are handled automatically. Additionally, the engine translates the *\[Flags\]* enums into GraphQL List-of-Enum types.

You can use business model entity classes directly as GraphQL types when you see fit. Most common cases are enums and Input types. 

After we register all types in module constructor, we need to map GraphQL Object types to to business logic layer entities: 

```csharp
      // in module constructor
      MapEntity<Starship>().To<Starship_>();
      MapEntity<Human>().To<Human_>(h => new Human_() { Mass = h.MassKg });
``` 

Most of the fields are matched by name. For mismatch cases, or when you need to call a function or do a conversion, you can provide an expression. More complex mappings are handled by resolvers. 
 
Resolvers are just instance methods in dedicated resolver classes registered with GraphQL module (see above). Not every field in GraphQL model need a resolver - only those that cannot be handled by property mapping to business entities. Among these - fields in top-level _Query_, _Mutation_ types, and all fields with arguments. A resolver is mapped to the field using either \[Resolver\] attribute (on the field), \[Resolves\] attribute on the resolver itself, or by a name match in absense of attributes or mapping. 

Here is a resolver method for the *length* field of the *Starship* type:
 
```csharp
    public float? GetLength(IFieldContext fieldContext, Starship starship, LengthUnit unit) {
      if (starship.Length == null)
        return null;
      else if (unit == LengthUnit.Meter)
        return starship.Length;
      else 
        return starship.Length * 3.28f; // feet
    }
``` 

The first parameter is always the *fieldContext*, the second is the parent entity for which the field is invoked (absent for top-level Query fields). The reference to the business layer objects is available through injection at resolver class initialization, or through _fieldContext_ reference to global context and GraphQL App.

With GraphQL module defined, we can now create GraphQL server and setup Http endpoints. Create a new ASP.NET Core API project, add references to _NGraphQL_, NGraphQL.Server_, _NGraphQL.Server.AspNetCore_ packages, and to the project containing the GraphQL classes we just defined. Add the following code to the _Startup_ class: 
  
```csharp
    private GraphQLHttpServer CreateGraphQLHttpServer() {
      var app = new StarWarsApp(); // or ref to your biz app
      var server = new GraphQLServer(app); 
      server.RegisterModules(new StarWarsApiModule());
      return new GraphQLHttpServer(server);
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      // ... skipped

      app.UseRouting();
      var server = CreateGraphQLHttpServer(); 
      app.UseEndpoints(endpoints => {
        endpoints.MapPost("graphql", async context => await server.HandleGraphQLHttpRequestAsync(context));
        endpoints.MapGet("graphql", async context => await server.HandleGraphQLHttpRequestAsync(context));
        endpoints.MapGet("graphql/schema", async context => await server.HandleGraphQLHttpRequestAsync(context));
     });
    }
``` 

We create HTTP server instance and setup the standard GraphQL HTTP endpoints. Launch the project - the GraphQL server will start.   

##Client


## Examples
The repo contains a [test application](tree/master/src/TestApp) with HTTP server and *Graphiql UI*. It is used in HTTP server harness and unit tests. It is a made-up GraphQL API about abstract *Things*, and it is void of any real semantic meaning. The sole purpose is to provide a number of types and methods covering the many aspects of the *GraphQL* protocol. Run the HTTP server harness and play with the *Graphiql* page in browser.

You can run unit tests and see the many request/response examples used there. The unit tests write a detailed log as they go. Run the tests, locate the log file in the *bin* folder, and browse the file for many examples of GraphQL requests and responses along with metrics. 

See also [Star Wars Example](https://github.com/rivantsov/starwars) in a separate github repository. 

## Documentation
See the [Wiki pages](https://rivantsov/ngraphql/wiki) for this project. 

##  Limitations
* *Code-first only, no schema-first scenario*. Implementing a working GraphQL API requires creating a number of detailed c#/.NET artefacts that cannot be directly derived from the Schema document. The complete schema-first scenario is not feasible.

* *Subscriptions are not implemented* - coming in the future

## System requirements
Visual Studio 2019, .NET Standard 2.0, .NET Core 3.1 

## Other GraphQL on .NET solutions
* [GraphQL DotNet](https://github.com/graphql-dotnet/graphql-dotnet)
* [HotChocolate](https://github.com/ChilliCream/hotchocolate)
* [Tanka GraphQL](https://github.com/pekkah/tanka-graphql)

