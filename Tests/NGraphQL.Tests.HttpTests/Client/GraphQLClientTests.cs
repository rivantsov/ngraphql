using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;

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
      var vars = new TDict() { { "id", 2 } };

      // Post requests
      TestEnv.LogTestDescr("Basic test for returned strong-type");
      query = @"
query ($id: Int) { 
  thing: getThing(id: $id) {
    id, name, kind, getRandoms(count: 5) 
  } 
}";
      resp = await TestEnv.Client.PostAsync(query, vars);
      resp.EnsureNoErrors();
      var thing = resp.GetField<Thing>("thing");
      Assert.IsNotNull(thing);
      Assert.AreEqual("Name2", thing.Name, "thing name mismatch");
      Assert.IsNotNull(thing.Randoms, "Expected randoms array");
      Assert.AreEqual(5, thing.Randoms.Length, "expected 5 randoms");
    }
  } //class
}
