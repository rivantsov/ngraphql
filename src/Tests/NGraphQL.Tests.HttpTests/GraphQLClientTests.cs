using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using NGraphQL.Introspection;

using Things;
using Things.GraphQL.Types;

namespace NGraphQL.Tests.HttpTests.Client {
  using TDict = Dictionary<string, object>;

  [TestClass]
  public class GraphQLClientTests {

    [TestInitialize]
    public void Init() {
      TestEnv.Initialize();
    }

    [TestMethod]
    public async Task TestGraphQLClient() {
      TestEnv.LogTestMethodStart();
      GraphQLResult gResult;
      string thingName;
      var query1 = "query ($id: Int) { getThing(id: $id) {name description kind theFlags} }";
      var queryM = "query { things {name} }";
      var vars = new TDict() { { "id", 3 } };

      // Post requests
      TestEnv.LogTestDescr("Testing Post requests");
      // single thing with query parameter
      gResult = await TestEnv.Client.PostAsync(query1, vars);
      gResult.EnsureNoErrors();
      var thing = gResult.GetTopField<Thing>("getThing");
      thingName = thing.Name;
      Assert.AreEqual("Name3", thingName);
      Assert.AreEqual(ThingKind.KindThree, thing.Kind, "Invalid kind field value.");
      Assert.AreEqual(TheFlags.FlagOne | TheFlags.FlagTwo, thing.TheFlags, "Invalid flags field value");

      // list of things 
      gResult = await TestEnv.Client.PostAsync(queryM, vars);
      gResult.EnsureNoErrors();
      var things = gResult.GetTopField<Thing[]>("things");
      thingName = things[1].Name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing Get requests");
      // single thing with query parameter
      gResult = await TestEnv.Client.GetAsync(query1, vars);
      gResult.EnsureNoErrors();
      thing = gResult.GetTopField<Thing>("getThing");
      Assert.AreEqual("Name3", thing.Name);

      // list of things 
      gResult = await TestEnv.Client.GetAsync(queryM, vars);
      gResult.EnsureNoErrors();
      things = gResult.GetTopField<Thing[]>("things");
      thingName = things[1].Name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing queries with errors");
      gResult = await TestEnv.Client.PostAsync(query1 + " ABCD ", vars);
      var errs = gResult.Errors;
      Assert.IsTrue(errs.Count > 0, "Expected syntax error");
    }


    [TestMethod]
    public async Task TestGraphQLClient_StrongTypes() {
      TestEnv.LogTestMethodStart();
      GraphQLResult result;
      string query;
      var vars = new TDict() { { "id", 3 } };

      // Post requests
      TestEnv.LogTestDescr("Basic test for strongly-typed return value.");
      query = @"
query ($id: Int) { 
  thing: getThing(id: $id) {
    id, name, kind, theFlags, randoms(count: 5), __typename
  } 
}";
      result = await TestEnv.Client.PostAsync(query, vars);
      result.EnsureNoErrors();
      var thing = result.GetTopField<Thing>("thing");
      Assert.IsNotNull(thing);
      Assert.AreEqual("Name3", thing.Name, "thing name mismatch");
      Assert.AreEqual(ThingKind.KindThree, thing.Kind, "Kind mismatch");
      Assert.AreEqual(TheFlags.FlagOne | TheFlags.FlagTwo, thing.TheFlags, "Flags mismatch");
      Assert.IsNotNull(thing.Randoms, "Expected randoms array");
      Assert.AreEqual(5, thing.Randoms.Length, "expected 5 randoms");
      
      // Check unmapped introspection field - to be implemented
      // string typeName = gResult.GetUnmappedFieldValue<string>(thing, "__typename");
      // Assert.AreEqual("ThingEntity", typeName, "type name does not match");
    }

    [TestMethod]
    public async Task TestGraphQLClient_Introspection() {
      TestEnv.LogTestMethodStart();
      GraphQLResult result;
      string query;
       
      // Post requests
      TestEnv.LogTestDescr("Querying type object for Thing type.");
      query = @"
query { 
  thingType: __type(name: ""Thing"") {
    name
    fields {
      name
      type {
        name
        displayName # NGraphQL extension
      }
    }
  } 
}";
      result = await TestEnv.Client.PostAsync(query);
      result.EnsureNoErrors();
      var type = result.GetTopField<__Type>("thingType");
      Assert.IsNotNull(type);
      Assert.AreEqual("Thing", type.Name, "thing name mismatch");
      Assert.IsTrue(type.Fields.Count > 5, "Expected fields");
    }


  } //class
}
