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

```c$
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

We use the underscore suffix (\_) in type names to avoid name collisions with the underlying 'business' entities. This prefix will be automatically stripped by the engine in Schema definition. The XML comments will appear in the Schema document as GraphQL descriptions. The \[Null\] attribute marks the field as nullable; everything is non-nullable by default, except nullable value types like *int?*. 

The top-level Query, Mutation types are defined as an interface:      
```c$
  interface IStarWarsQuery {

    [Resolver("GetStarships")]
    IList<Starship_> Starships { get; }

    [GraphQLName("starship"), Resolver("GetStarshipAsync")]
    Starship_ GetStarship([Scalar("ID")] string id);
  }
``` 

Once you defined all types, interfaces,  unions etc, you register them as part of a GraphQL module: 

```c$
  public class StarWarsApiModule: GraphQLModule {
    public StarWarsApiModule() {
      // Register types
      this.EnumTypes.AddRange(new Type[] { typeof(Episode), typeof(LengthUnit), typeof(Emojis) });
      this.ObjectTypes.AddRange(new Type[] { typeof(Human_), typeof(Droid_), typeof(Starship_), typeof(Review_) });
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

### Client


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

