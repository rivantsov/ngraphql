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
  /// <summary>A humanoid creature from the Star Wars universe </summary>
  public class Human_ : ICharacter_ {
    /// <summary>The ID of the human </summary>
    [Scalar("ID")]
    public string Id { get; set; }

    /// <summary>What this human calls themselves </summary>
    public string Name { get; set; }

    /// <summary>This human's friends, or an empty list if they have none </summary>
    public IList<ICharacter_> Friends { get; }

    /// <summary>The movies this human appears in </summary>
    public IList<Episode> AppearsIn { get; }

    /// <summary>The home planet of the human, or null if unknown </summary>
    [Null] public string HomePlanet { get; set; }

    /// <summary>Height in the preferred unit, default is meters </summary>
    [GraphQLName("height")]
    public float? GetHeight(LengthUnit unit = LengthUnit.Meter) { return default; }

    /// <summary>Mass in kilograms, or null if unknown </summary>
    public float? Mass { get; set; }

    /// <summary>A list of starships this person has piloted, or an empty list if none </summary>
    [Resolver("GetStarshipsBatched")] 
    public IList<Starship_> Starships { get; }
  }
``` 

We use the underscore suffix (\_) in type names to avoid name collisions with the underlying 'business' entities. This prefix will be automatically stripped by the engine in Schema definition. The XML comments will appear in the Schema document as GraphQL descriptions. The \[Null\] attribute marks the field as nullable; everything is non-nullable by default, except nullable value types like *int?*. 

Once you defined all    

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

