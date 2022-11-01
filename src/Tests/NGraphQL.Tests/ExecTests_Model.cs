using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;
using NGraphQL.Server;
using System.Collections;
using Things.GraphQL.Types;
using IDict = System.Collections.Generic.IDictionary<string, object>;
using Dict = System.Collections.Generic.Dictionary<string, object>;

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


    // Used in Map scalar test
    public class SomeObj {
      public string Name;
      public int Value;
      public SomeObj Next;
      public override string ToString() {
        return $"{Name} {Value} {Next?.ToString()}";
      }
    } //method


  } //class
}
