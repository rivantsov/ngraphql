using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using NGraphQL.Internals;
using NGraphQL.Introspection;
using NGraphQL.TestApp;

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
      ServerResponse resp;
      string thingName;
      var query1 = "query ($id: Int) { getThing(id: $id) {name kind theFlags} }";
      var queryM = "query { things {name} }";
      var vars = new TDict() { { "id", 3 } };

      // Post requests
      TestEnv.LogTestDescr("Testing Post requests");
      // single thing with query parameter
      resp = await TestEnv.Client.PostAsync(query1, vars);
      resp.EnsureNoErrors();
      var thing = resp.data.getThing;
      thingName = thing.name;
      Assert.AreEqual("Name3", thingName);
      var thingKind = EnumConvert.ToEnum<ThingKind>(thing.kind);
      Assert.AreEqual(ThingKind.KindThree, thingKind, "Invalid kind field value.");
      var flags = EnumConvert.ToEnum<TheFlags>(thing.theFlags);
      Assert.AreEqual(TheFlags.FlagOne | TheFlags.FlagTwo, flags, "Invalid flags field value");

      // list of things 
      resp = await TestEnv.Client.PostAsync(queryM, vars);
      resp.EnsureNoErrors();
      thingName = resp.data.things[1].name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing Get requests");
      // single thing with query parameter
      resp = await TestEnv.Client.GetAsync(query1, vars);
      resp.EnsureNoErrors();
      thingName = resp.data.getThing.name;
      Assert.AreEqual("Name3", thingName);

      // list of things 
      resp = await TestEnv.Client.GetAsync(queryM, vars);
      resp.EnsureNoErrors();
      thingName = resp.data.things[1].name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing queries with errors");
      resp = await TestEnv.Client.PostAsync(query1 + " ABCD ", vars);
      var errs = resp.Errors;
      Assert.IsTrue(errs.Count > 0, "Expected syntax error");
    }


    [TestMethod]
    public async Task TestGraphQLClient_StrongTypes() {
      TestEnv.LogTestMethodStart();
      ServerResponse resp;
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
      resp = await TestEnv.Client.PostAsync(query, vars);
      resp.EnsureNoErrors();
      var thing = resp.GetTopField<Thing_>("thing");
      Assert.IsNotNull(thing);
      Assert.AreEqual("Name3", thing.Name, "thing name mismatch");
      Assert.AreEqual(ThingKind.KindThree, thing.Kind, "Kind mismatch");
      Assert.AreEqual(TheFlags.FlagOne | TheFlags.FlagTwo, thing.TheFlags, "Flags mismatch");
      Assert.IsNotNull(thing.Randoms, "Expected randoms array");
      Assert.AreEqual(5, thing.Randoms.Length, "expected 5 randoms");
      
      // Check unmapped introspection field - to be implemented
      // string typeName = resp.GetUnmappedFieldValue<string>(thing, "__typename");
      // Assert.AreEqual("Thing", typeName, "type name does not match");
    }

    [TestMethod]
    public async Task TestGraphQLClient_Introspection() {
      TestEnv.LogTestMethodStart();
      ServerResponse resp;
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
      resp = await TestEnv.Client.PostAsync(query);
      resp.EnsureNoErrors();
      var type = resp.GetTopField<__Type>("thingType");
      Assert.IsNotNull(type);
      Assert.AreEqual("Thing", type.Name, "thing name mismatch");
      Assert.IsTrue(type.Fields.Count > 5, "Expected fields");
    }


  } //class
}
