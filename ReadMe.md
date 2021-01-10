# NGraphQL - GraphQL for .NET 

**NGraphQL** is a set of packages implementing [GraphQL APIs](https://spec.graphql.org/) client and server components in .NET.  

## Documentation
See [wiki pages](ttps://rivantsov/ngraphql/wiki). 

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


## Examples
The repo contains a [test application](src/testapp/) with HTTP server and *Graphiql UI*. It is used in HTTP server harness and unit tests. It is a made-up GraphQL API about abstract *Things*, and it is void of any real semantic meaning. The sole purpose is to provide a number of types and methods covering the many aspects of the *GraphQL* protocol. Run the HTTP server harness and play with the *Graphiql* page in browser.

You can run unit tests and see the many request/response examples used there. The unit tests write a detailed log as they go. Run the tests, locate the log file in the *bin* folder, and browse the file for many examples of GraphQL requests and responses along with metrics. 

See also [Star Wars Example](https://github.com/rivantsov/starwars) in a separate github repository. 

##  Limitations
* *Code-first only, no schema-first scenario*. Implementing a working GraphQL API requires creating a number of detailed c#/.NET artefacts that cannot be directly derived from the Schema document. The complete schema-first scenario is not feasible.

* *Subscriptions are not implemented* - coming in the future

## System requirements
Visual Studio 2019, .NET Standard 2.0, .NET Core 3.1 

## Other GraphQL on .NET solutions
* [GraphQL DotNet](https://github.com/graphql-dotnet/graphql-dotnet)
* [HotChocolate](https://github.com/ChilliCream/hotchocolate)
* [Tanka GraphQL](https://github.com/pekkah/tanka-graphql)

