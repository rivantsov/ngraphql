# NGraphQL - GraphQL for .NET 

**NGraphQL** is a framework for implementing [GraphQL](https://graphql.org/) APIs in .NET. It provides server- and client-side components.  
Here is an [overview of the project](https://rivantsov.medium.com/ngraphql-a-new-framework-for-building-graphql-solutions-in-net-44a460d97e3c), what is different and why I created it in the first place. 

## Features
* Conforms to [GraphQL Specification, Oct 2021 Edition](https://spec.graphql.org/October2021/).
* GraphQL model is defined using plain c# (POCO) classes decorated with some attributes. Unlike other .NET GraphQL solutions, *NGraphQL*-based API definitions look and feel like real .NET artifacts - strongly typed, compact and readable.   
* Server and client components. ASP.NET Core -based HTTP server implementation following the standard "serving over HTTP" rules 
* Subscriptions are fully supported
* Light-weight but capable GraphQL Client - supports strongly-type objects for the returned data. Easy client-side API for subscriptions 
* Modular construction - separately coded modules define parts of the overall GraphQL API Schema; modules are registered with the GraphQL host server which implements the GraphQL API. 
* Supports parallel execution of Query requests
* Sync and Async resolver methods
* Full Introspection support
* Schema descriptions are automatically imported from XML comments in c# code
* Full support for fragments and standard directives (@include, @skip, @deprecated)
* Custom Scalars out of the box (Double, ID, Uuid, DateTime etc)
* Fast, efficient query parser; query cache - parsed queries are saved in cache for future reuse with different input variables
* Facilities for input validation and returning failures as multiple GraphQL errors
* Robust implementation of batching (N+1 problem)
* Integration with relational databases and ORMs - the [BookStore Sample](https://github.com/rivantsov/vita) shows a GraphQL server on top of a data-connected application, with batching support.    

## Packages and Components
NGraphQL binaries are distributed as a set of [NuGet packages](https://www.nuget.org/packages/NGraphQL/):

|Package|Description
|-------|-----------
|NGraphQL|Basic classes shared by client and server components.|
|NGraphQL.Client|GraphQL client.|
|NGraphQL.Server|GraphQL server implementation not tied to a specific transport protocol.|
|NGraphQL.Server.AspNetCore|GraphQL HTTP server based on ASP.NET Core stack.|

## Examples
The repo contains a Test project with HTTP server: Things.GraphQL.HttpServer. You can launch it directly as a startup project in Visual Studio.

Install the GraphQL Playground for Chrome extension from Chrome store, and launch the project. It will start the web server, and will open the GraphQL Playground page. Enter the following URL as the target: http://localhost:55571/graphql, and run a sample query: "query { things {name kind theFlags aBCGuids} }". The test server implements a GraphQL API about abstract *Things*, and it is void of any real semantic meaning - it is for testing purpose only. The purpose of this app is to provide a number of types and methods covering the many aspects of the *GraphQL* protocol. 

Run the **unit tests** and see the many request/response examples used there. The unit tests write a detailed log as they go. Run the tests, locate the log file in the *bin* folder, and look inside for many examples of GraphQL requests and responses along with the metrics. See this file here: [UnitTestsLog](misc/UnitTestsLog.txt).

See also [Star Wars Example](https://github.com/rivantsov/starwars) in a separate github repository. 

[VITA](https://github.com/rivantsov/vita) ORM contains a sample project implementing a GraphQL Server for a BookStore sample application. Among other things, it shows how (N+1) problem can be efficiently handled **automagically** by a smart-enough ORM. Most of the related entities like *Book.Publisher* or *Book.Authors* are batch-loaded automatically by the ORM. 

## Documentation
See the [Wiki pages](https://github.com/rivantsov/ngraphql/wiki) for this project. 

## System requirements
.NET Standard 2.0, .NET 6/8.  

## Other GraphQL on .NET solutions
* [GraphQL DotNet](https://github.com/graphql-dotnet/graphql-dotnet)
* [HotChocolate](https://github.com/ChilliCream/hotchocolate)
* [Tanka GraphQL](https://github.com/pekkah/tanka-graphql)

