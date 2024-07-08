# Code Changes History

## July 8, 2024. Version 2.0.0


## April 12, 2024. Version 1.7.0
- Switched Json serialization to System.Text.Json package (from good old NewtonSoft). Support for dynamic return types (keyword dynamic) in GraphQLClient is dropped since this new serializer does not support it (fully). 
- Upgraded dependent package references. Microsoft changed the distribution model for AspNetCore (no longer in packages but in one App bundle), so I could not find a way for the package NGraphQL.Server.AspNetCore to stay at .NET Standard. Now this package/assembly is double-targeted to Net6 and Net8. The rest of the packages are still on .NET Standard 2.0
- Added extension methods for GraphQL server setup, so now you can configure the server using a typical pattern; the code is shorter: 
```c#
  builder.AddGraphQLServer(graphQLServer); 
  ...
  app.MapGraphQLEndpoint(); 

``` 
The default GraphQLController that handles GraphQL requests is provided with the NGraphQL.Server.AspNetCore package and is configured with the methods shown above. 
- Name changes: GraphQLHttpServer -> GraphQLHttpHandler; ServerResponse -> GraphQLResult 
- Fixed a minor bug in introspection queries, now introspection types/fields themselves are not returned in introspection queries. The clients like Graphiql and Altair were complaining about this. 
- Removed dependency on Graphiql package to display GraphQL visual tool. Now the test app (Things.GraphQL.HttpServer project) opens the page of Chrome Altair extension. Make sure you install it in Chrome before launching the app. 
- 

## Oct 31, 2022. Version 1.5.0
Implementation of long-asked feature in GraphQL - allow Input types to be used as (output) Object types. So you can use Input type as field type in Object types, including methods return types. But still, Object types may not be used in Input types. Essentially Input types are now treated as restricted Object types, Input types are subset of Object types. 

There are often situations when you have a simple data object, and you want to use it in both directions - as server input and output of fields and methods. But you can't - there's this strange restriction in the current GraphQL spec. You end up defining two identical types, one for input, one for output.  NGraphQL now drops this restriction. It DOES NOT break any compatibility, it is non-breaking extension. If you don't like it - just don't use it.

There are also a few minor fixes in this release, a bit of refactoring in unit tests.

## Oct 9, 2022. Version 1.4.2
A minor release with small fixes, dependencies upgrades
- Upgraded all 'apps' to Net6 (from Core 3.1 which is oudated). All nuget packages/libs remain at .Net Standard 2.0, for most compatibility.
- Upgraded Newtonsoft.Json lib to 13.0.1 (required for discovered vulnerability in previous versions)
- Small refactorings, few serialization/deserialization fixes.  

## April 24, 2022. Version 1.4.0
A number of features to reach formal compliance with GraphQL Specification, 2021 version. 
- Allow interfaces implement other interfaces
- Implementation of an interface (Object type or another interface) can have covariant field types - not necessarily exactly matching, but 'sub-classed'. Improvement in NGraphQL: c# does not allow covariant implementation of interfaces, so you cannot use c# inheritance to express this GraphQL relationship. Now you can use _ImplementsAttribute_ on implementor type to point to another interface that is implemented in 'covariant' manner. 
- Fields results merging for complex fields
- _SpecifiedBy(url)_ attribute and introspection field for custom scalars. 
- Repeatable directives and _repeatable_ keyword in SDL files
- Allowing directives on variable definitions

## Oct 1, 2021. Versions 1.3.0
Map Scalar type (Dictionary<string, object>)

## June 7, 2021. Versions 1.2.1
Minor bug fix - allow very large number literals for decimal values

## June 3, 2021. Versions 1.2.0
Internal refactoring and bug fixes:
- mapping entities to multiple GraphQL types;
- new infrastructure for supporting standard and custom directives
- improved integration with VITA ORM, added RequestContext.VitaOperationContext field; set by VitaWebMiddleWare if configured in ASP.NET Core server. Improves integration with VITA authentication
- Added option to suppress parallel execution - strongly recommended option for database-connected applications
- multiple bug fixes 

## Feb 21, 2021. Versions 1.1.2
Patch, bug fix for issue: incorrect handling mapping expressions when field expression returns GraphQL type directly.

## Feb 14, 2021. Versions 1.1.0, 1.1.1
Mostly internal cleanup and refactoring. Some changes are part of an effort to integrate with DB-connected apps. 
A sample "GraphQL-over-database" project is published as part of [VITA ORM repo](https://github.com/rivantsov/vita).

## Jan 10, 2021. Version 1.0 Release
Server and client components, samples, GraphiQL integration.

