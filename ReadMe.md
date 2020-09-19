# NGraphQL - GraphQL implementation for .NET 
 **Performance. Simplicity. Usability.** 

**NGraphQL** is a library for implementing [GraphQL APIs](https://spec.graphql.org/) in .NET. 

## NGraphQL Highlights
* Follows **GraphQL specification**, June 2018 Edition
* **Usability**: relative simple and straightforward API development 
* **High performance - sub-millisecond roundtrip times** for a typical query 
* Automatic handling of **GraphQL Enums** - automatic conversion of .NET Enum values to ALL_CAPS strings and back (MyEnumValue <-> MY_ENUM_VALUE)
* Support for **Enum list types** - mapped to **c# \[Flags\] enums**, conversion is handled automatically.
* **Parsed query cache**. Repeated identical queries are not parsed again, but taken from the cache and executed with different variable values
* **Http Server** implementation, ready to use in an ASP.NET Core Web application. 
* **Parallel execution** of queries
* Efficient, intuitive **Batching** implementation (aka **Data Loader** ). 
* Support for easy **input validation** and adding **GraphQL errors** (if any) to the response; aborting the request if errors detected.

## Exploring the source code
Download the source code, open the solution in *Visual Studio 2019*. Build all and run unit tests in *NGraphQL.Tests* project - all tests should pass. 

The unit tests write a **detailed log** as they go - and this log is quite readable. Open the log file *_graphQLtests.log* in the *bin* folder of the test project (*HttpTests* project has a similar log file). Scroll through the file - for every test there is a test description, request and response printout (with errors if any), and execution metrics. 

Notice the metrics numbers - once the paths are warmed up, a **typical query runs under 1 ms** - which includes deserializatio, parsing, execution (none for the test app) and serialization of results. 

## TestApp application
The tests use a simple test app and its GraphQL API in the project NGraphQL.TestApp. The app/API defined there is completely void of any real-world semantics - just abstract THINGS and other things etc. No *StarWars* or any real/imaginary world story. Just a playground with made up silly named artefacts for testing GraphQL features.
The *TestApp* project contains 2 areas - a 'business app' with some data 'entities', and a GraphQL API built on top of it, to expose the 'entities' of the business app. Explore the project sources to get the general idea of how it works. 

## API development: Code-first only, no schema-first
It turned out, implementing a working GraphQL API requires creating a number of *detailed c#/.NET artefacts that may not be directly derived or guessed from the Schema document*. So for an existing schema/.net code pair, it is not possible to derive necessary .NET code change from GraphQL Schema document changes. The **complete schema-first scenario is not feasible**.

## Documentation - coming soon
For now, please look at the _TestApp_ project code, and unit tests. 

## Nuget packages
NGraphQL comes in 2 nuget packages: 
* **NGraphQL** - basic components for building custom GraphQL APIs; GraphQL Server implementation
* **NGraphQL.Http** - Http Server on top of GraphQL server

## System requirements
Visual Studio 2019, .NET Standard 2.0, 

