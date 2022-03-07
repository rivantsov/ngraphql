# NGraphQL - GraphQL for .NET 

**NGraphQL** is a framework for implementing [GraphQL](https://graphql.org/) APIs in .NET. It provides server- and client-side components.  

## Features
* Implements Official 2018 Specification.
* GraphQL model is defined using plain c# (POCO) classes decorated with some attributes. Unlike other .NET GraphQL solutions, *NGraphQL*-based API definitions look and feel like real .NET artifacts - strongly typed, compact and readable.   
* Server and client components. ASP.NET Core -based HTTP server implementation following the standard "serving over HTTP" rules 
* Light-weight but capable GraphQL Client - supports both dynamic-type objects for return data, or strongly-typed objects by directly using the GraphQL c# classes from the model. 
* Modular construction - separately coded modules define parts of the overall GraphQL API Schema; modules are registered with the GraphQL host server which implements the GraphQL API. 
* Parallel execution of Query requests
* Sync and Async resolver methods
* Full Introspection support
* Schema descriptions are automatically imported from XML comments in c# code
* Full support for fragments and standard directives (@include, @skip, @deprecated)
* Custom Scalars out of the box (Double, ID, Uuid, DateTime etc)
* Fast, efficient query parser; query cache - parsed queries are saved in cache for future reuse with different input variables
* Facilities for input validation and returning failures as multiple GraphQL errors
* Robust implementation of batching (N+1 problem)
* Integration with relational databases and ORMs - the [BookStore Sample](https://github.com/rivantsov/vita) shows a GraphQL server on top of a data-connected application, with batching support.    
* Built-in logging and diagnostics, query timings and metrics

## Packages and Components
NGraphQL binaries are distributed as a set of [NuGet packages](https://www.nuget.org/packages/NGraphQL/):

|Package|Description|Size,KB|
|-------|-----------|------|
|NGraphQL|Basic classes shared by client and server components.|23|
|NGraphQL.Client|GraphQL client.|21|
|NGraphQL.Server|GraphQL server implementation not tied to a specific transport protocol.|174|
|NGraphQL.Server.AspNetCore|GraphQL HTTP server based on ASP.NET Core stack.|23|

## Examples
The repo contains a TestApp with HTTP server and *Graphiql UI*. It is used in HTTP server harness and unit tests. It is a made-up GraphQL API about abstract *Things*, and it is void of any real semantic meaning. The sole purpose of this app is to provide a number of types and methods covering the many aspects of the *GraphQL* protocol. Run the HTTP server harness and play with the *Graphiql* page in browser.

Run the **unit tests** and see the many request/response examples used there. The unit tests write a detailed log as they go. Run the tests, locate the log file in the *bin* folder, and look inside for many examples of GraphQL requests and responses along with the metrics. See this file here: [UnitTestsLog](misc/UnitTestsLog.txt).

See also [Star Wars Example](https://github.com/rivantsov/starwars) in a separate github repository. 

[VITA](https://github.com/rivantsov/vita) ORM contains a sample project implementing a GraphQL Server for a BookStore sample application. Among other things, it shows how (N+1) problem can be efficiently handled **automagically** by a smart-enough ORM. Most of the related entities like *Book.Publisher* or *Book.Authors* are batch-loaded automatically by the ORM. 

## Documentation
See the [Wiki pages](https://github.com/rivantsov/ngraphql/wiki) for this project. 

##  Limitations
* *Code-first only, no schema-first scenario*. Implementing a working GraphQL API requires creating a number of detailed c#/.NET artefacts that cannot be directly derived from the Schema document. The complete schema-first scenario is not feasible.

* Subscriptions are not implemented yet - coming in the future

## System requirements
Visual Studio 2019, .NET Standard 2.0, .NET Core 3.1 

## Other GraphQL on .NET solutions
* [GraphQL DotNet](https://github.com/graphql-dotnet/graphql-dotnet)
* [HotChocolate](https://github.com/ChilliCream/hotchocolate)
* [Tanka GraphQL](https://github.com/pekkah/tanka-graphql)

