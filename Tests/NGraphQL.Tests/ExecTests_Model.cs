using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;
using NGraphQL.Server;
using System.Collections;

namespace NGraphQL.Tests {

  public partial class ExecTests {

    [TestMethod]
    public void Test_Model_SchemaGenerateParse() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" schema doc generator; generating schema and parsing it, verifying syntactic correctness. See schema saved in the _thingsApiSchema.txt file in bin folder. ");
      var schemaDoc = TestEnv.ThingsServer.Model.SchemaDoc;
      // Try parsing the schema doc
      var parser = TestEnv.ThingsServer.Grammar.CreateSchemaParser();
      var schemaParseTree = parser.Parse(schemaDoc);
      Assert.IsFalse(schemaParseTree.HasErrors(), "expected no schema parsing errors.");
    }

    [TestMethod]
    public async Task Test_Model_CustomScalars() {
      TestEnv.LogTestMethodStart();


      TestEnv.LogTestDescr(@" custom scalars: Uuid, Decimal");
      var query = @"
query { 
  res: echoCustomScalars(dec: -12345.78, uuid: 'e675af6b-a421-43ef-98f4-e155df7ab8f6')
}";
      var resp = await ExecuteAsync(query);
      var result = (string)resp.Data["res"];
      var expected = "-12345.78|e675af6b-a421-43ef-98f4-e155df7ab8f6";
      Assert.AreEqual(expected, result, "Result mismatch");

      TestEnv.LogTestDescr(@" custom scalars: Date, Time");
      query = @"
query { 
  res: echoDateTimeScalars(dt: '2020/05/31', date: '2020/06/15', time: '11:22:33')
}";
      resp = await ExecuteAsync(query);
      result = (string)resp.Data["res"];
      expected = "2020-05-31|2020-06-15|11:22:33";
      Assert.AreEqual(expected, result, "Result mismatch");
    }

    [TestMethod]
    public async Task Test_Model_Introspection() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" introspection queries.");
      var introQuery = @"
query {
  ThingType: __type(name: ""Thing"") {
     name 
     fields { 
       name, 
       type { displayName }           # displayName is our extension to spec 
     }
  }
  typeKind: __type(name: ""__TypeKind"") {
     name enumValues  (includeDeprecated: true) {name}
  }
} ";
      var resp = await ExecuteAsync(introQuery); // just check it goes ok

      TestEnv.LogTestDescr(@" introspection queries, checking isDeprecated and and deprecationReason fields.");
      introQuery = @"
query introQuery {
  inputObjType: __type (name: ""InputObj"") {
    name
    inputFields {
      name
      isDeprecated
      deprecationReason
    }
  }
}
";
      resp = await ExecuteAsync(introQuery);
      var inpObj = resp.Data["inputObjType"];
      Assert.IsNotNull(inpObj, "Expected input obj type");



      TestEnv.LogTestDescr(@" Introspection, querying all __schema fields");
      introQuery = @"
query introQuery {
  __schema {
      queryType         { name fields { name } }
      mutationType      { name fields { name } }
      subscriptionType  { name fields { name } }
      types  { name }
      directives { name }
  }
}
";
      resp = await ExecuteAsync(introQuery);
      var typeList = resp.GetValue<IList>("__schema.types");
      Assert.IsTrue(typeList.Count > 5, "Expected types");

    } //method

  }
}
