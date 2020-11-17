using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using NGraphQL.Server;
using NGraphQL.Utilities;

namespace NGraphQL.Tests.HttpTests {
  using TDict = Dictionary<string, object>;

  [TestClass]
  public class HttpServerTests {

    [TestInitialize]
    public void Init() {
      TestEnv.Initialize();
    }


    [TestMethod]
    public async Task TestGetSchema() {
      TestEnv.LogTestMethodStart();

      // var schema = await TestEnv.RestClient.GetStringAsync("/schema");
      var schema = await TestEnv.Client.GetSchemaDocument(); 
      Assert.IsTrue(!string.IsNullOrWhiteSpace(schema), "expected schema doc");
      TestEnv.LogText("  Success: received Schema doc from server using endpoint '.../schema' ");
    }

    [TestMethod]
    public async Task TestGraphQLClient() {
      TestEnv.LogTestMethodStart();
      ServerResponse resp;
      string thingName;
      var query1 = "query ($id: Int) { getThing(id: $id) {name} }";
      var queryM = "query { things {name} }";
      var vars = new TDict() { { "id", 2 } };
      
      // Post requests
      TestEnv.LogTestDescr("Testing Post requests");
      // single thing with query parameter
      resp = await TestEnv.Client.PostAsync(query1, vars);
      thingName = resp.Data.getThing.name;
      Assert.AreEqual("Name2", thingName);

      // list of things 
      resp = await TestEnv.Client.PostAsync(queryM, vars);
      thingName = resp.Data.things[1].name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing Get requests");
      // single thing with query parameter
      resp = await TestEnv.Client.GetAsync(query1, vars);
      thingName = resp.Data.getThing.name;
      Assert.AreEqual("Name2", thingName);

      // list of things 
      resp = await TestEnv.Client.GetAsync(queryM, vars);
      thingName = resp.Data.things[1].name;
      Assert.AreEqual("Name2", thingName);

      TestEnv.LogTestDescr("Testing queries with errors");
      resp = await TestEnv.Client.PostAsync(query1 + " ABCD ", vars);
      var errs = resp.Errors;
      Assert.IsTrue(errs.Count > 0, "Expected syntax error");
    }

    [TestMethod]
    public async Task TestBasicQueries() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr("Testing dynamic query");
      var resp = await TestEnv.Client.PostAsync("query { things {name} }");
      var thing0Name = resp.Data.things[0].name;
      Assert.IsNotNull(resp);


      TestEnv.LogTestDescr("successful simple query."); 
      resp = await TestEnv.Client.PostAsync("query { things {name} }");
      Assert.IsNotNull(resp);

      TestEnv.LogTestDescr("invalid query");
      // invalid query - things field needs selection subset
      var errResp = await TestEnv.Client.PostAsync("query { things  }");
      Assert.IsNotNull(errResp);
      Assert.IsTrue(errResp.Errors.Count > 0);
    }

    [TestMethod]
    public async Task TestEnumsDeserialization() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr("input object with enum fields.");
      var query = @"
query ($objWithEnums: InputObjWithEnums) {
   echo: echoInputObjWithEnums(inpObj: $objWithEnums) 
}";
      var vars = new TDict() {
        { "objWithEnums", new TDict() {
                    { "flags", new string[] { "FLAG_ONE", "FLAG_THREE" } },
                    { "flagsArray", new object[] {
                           new [] {"FLAG_ONE", "FLAG_TWO" },
                           new [] {"FLAG_THREE" }
                      }},
                    { "kind", "KIND_TWO" },
                    }
        }, 
      };

      var resp = await TestEnv.Client.PostAsync(query, vars);
      Assert.IsNotNull(resp);
      var theFlagsStr = (string) resp.Data.echo;
      theFlagsStr = theFlagsStr.Replace(" ", string.Empty);
      Assert.AreEqual("Flags:FlagOne,FlagThree;kind:KindTwo;FlagsArray:[FlagOne,FlagTwo;FlagThree]", theFlagsStr, 
        "Invalid inputObjWithEnums echo");
    }


    [TestMethod]
    public async Task TestVariables() {
      string query;
      TDict varsDict;
      ServerResponse resp;

      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr("input variables of various types.");
      query = @"
query myQuery($boolVal: Boolean, $longVal: Long, $doubleVal: Double, $strVal: String, $kindVal: ThingKind, $flags: [TheFlags!]) { 
  echo: echoInputValuesWithNulls (boolVal: $boolVal, longVal: $longVal, doubleVal: $doubleVal, strVal: $strVal, 
                                  kindVal: $kindVal, flags: $flags )
}";
      varsDict = new TDict() {
        { "boolVal", true }, { "longVal", 654321 }, { "doubleVal", 543.21 },
        { "kindVal", "KIND_ONE" }, {"flags", new string[] {"FLAG_ONE", "FLAG_TWO"}},
        { "strVal", "SomeString" }
      };
      resp = await TestEnv.Client.PostAsync(query, varsDict);
      var echoResp = resp.Data.echo;
      Assert.AreEqual("True|654321|543.21|SomeString|KindOne|FlagOne, FlagTwo", echoResp); //this is InputObj.ToString()

      TestEnv.LogTestDescr("error - invalid argument values, type mismatch.");
      query = @"
query myQuery($boolVal: Boolean, $longVal: Long, $doubleVal: Double, $strVal: String, $kindVal: ThingKind, $flags: [TheFlags!]) { 
  echo: echoInputValuesWithNulls (boolVal: $longVal, longVal: $doubleVal, doubleVal: $strVal )
}";
      resp = await TestEnv.Client.PostAsync(query, varsDict, throwOnError: false);
      Assert.AreEqual(3, resp.Errors.Count, "Expected 3 errors");

      TestEnv.LogTestDescr("complex object type in a variable.");
      query = @"
query myQuery($inpObj: InputObj!) { 
  echoInputObj (inpObj: $inpObj) 
}";
      varsDict = new TDict();
      varsDict["inpObj"] = new TDict() { { "id", 123 }, { "num", 456 }, { "name", "SomeName" } };
      resp = await TestEnv.Client.PostAsync(query, varsDict); 
      var echoInpObj = resp.Data.echoInputObj;
      Assert.AreEqual("id:123,name:SomeName,num:456", echoInpObj); //this is InputObj.ToString()

      TestEnv.LogTestDescr("literal object as argument, but with prop values coming from variables.");
      query = @"
query myQuery($num: Int!, $name: String!) { 
  echoInputObj (inpObj: {id: 123, num: $num, name: $name}) 
}";
      varsDict = new TDict();
      // we cannot use InputObj here, serializer will send first-cap prop names and request will fail
      varsDict["num"] = 456;
      varsDict["name"] = "SomeName";
      resp = await TestEnv.Client.PostAsync(query, varsDict);
      var echoInpObj2 = resp.Data.echoInputObj;
      Assert.AreEqual("id:123,name:SomeName,num:456", echoInpObj2); //this is InputObj.ToString()
    }

  }
}
