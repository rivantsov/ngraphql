using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using Things;
using Things.GraphQL.Types;

namespace NGraphQL.Tests.HttpTests {
  using IDict = System.Collections.Generic.IDictionary<string, object>;
  using Dict = System.Collections.Generic.Dictionary<string, object>;

  [TestClass]
  public class CustomScalarTests {

    [TestInitialize]
    public void Init() {
      TestEnv.Initialize();
    }

    // Used as data for Any, Map scalars test
    public class SomeObj {
      public string Name;
      public int Value;
      public SomeObj Next;
    }

    [TestMethod]
    public async Task Test_MapScalar() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" MapScalar test 1; returning Map in output field.");

      var query = @"
query { 
  res: getThing (id: 1) {
    id name 
    props { Prop1, Prop2 }  # there are 2 props in the obj, see TestDataGenerator
  }
}";
      var resp = await ExecuteAsync(query);
      var thing1 = resp.GetTopField<Thing>("res");
      var props = thing1.Props;
      //var props = (Dict)propsObj;
      Assert.AreEqual(2, props.Count, "Expected 2 props in Dict"); // 


      TestEnv.LogTestDescr(@" MapScalar test 2 - map in Input object.");
      query = @"
query ($inp: InputObjWithCustomScalars) { 
  res: echoInputObjWithCustomScalars (inp: $inp) 
                 {  map maxLong minLong }
}";
      var nestedMap = new Dict() { { "nestedStr", "NestedStr" }, { "nestedInt", 234 } };
      var map = new Dict() { { "prop1", "v1" }, { "prop2", 123 }, { "nullV", null }, { "nested", nestedMap } };
      var inpObj = new InputObjWithCustomScalars() {
        Map = map, MaxLong = long.MaxValue, MinLong = long.MinValue
      };
      var vars = new Dict() { { "inp", inpObj } };
      resp = await ExecuteAsync(query, vars);
      var inpBack = resp.GetTopField<InputObjWithCustomScalars>("res");
      Assert.AreEqual(4, inpBack.Map.Count, "Expected 4 props in Dict");
      // check nested dict and value inside
      var nested = (Dict) inpBack.Map["nested"];
      int nestedInt = (int)nested["nestedInt"];
      Assert.AreEqual(234, nestedInt);

      TestEnv.LogTestDescr(@" MapScalar test 3 - map in Input object; sending map value as literal (array of arrays).");
      query = @"
query { 
  res: echoInputObjWithCustomScalars (inp: {
            map: [ [""prop1"" ""v1""] [prop2 123] ]            # notice prop2 without quotes, it is allowed
       })
           {  map maxLong minLong }
}";
      resp = await ExecuteAsync(query);
      inpBack = resp.GetTopField<InputObjWithCustomScalars>("res");
      Assert.AreEqual(2, inpBack.Map.Count, "Expected 2 props in Map field ");

      TestEnv.LogTestDescr(@" MapScalar test 4 - map in Input object; sending map value as literal, formatted like input object.");
      query = @"
query { 
  res: echoInputObjWithCustomScalars (inp: {
            map: {prop1: ""v1"" prop2: 123}                     # formatted like InputObject literal
       })
                 {  map maxLong minLong }
}";
      resp = await ExecuteAsync(query);
      inpBack = resp.GetTopField<InputObjWithCustomScalars>("res");
      Assert.AreEqual(2, inpBack.Map.Count, "Expected 2 props in Dict");
    }

    public async Task<GraphQLResult> ExecuteAsync(string query, IDictionary<string, object> vars = null) {
      var resp = await TestEnv.Client.PostAsync(query, vars);
      resp.EnsureNoErrors();
      return resp; 
    }

    /* 
    [TestMethod]
    public async Task Test_AnyScalar() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" AnyScalar: returning various values in Any field.");
      var query = @"
query ($inp: [InputObjWithMapAny]!) { 
  res: echoInputObjectsWithAny (inp: $inp)
}";
      var someObj = new SomeObj {
        Name = "Parent", Value = 456, Next = new SomeObj { Name = "Child", Value = 567 }
      };
      var inp = new InputObjWithMapAny[] {
        new InputObjWithMapAny() {AnyValue = 123},
        new InputObjWithMapAny() {AnyValue = 12.34},
        new InputObjWithMapAny() {AnyValue = "abc"},
        new InputObjWithMapAny() {AnyValue = someObj},
        new InputObjWithMapAny() {AnyValue = true},
        new InputObjWithMapAny() {AnyValue = null},
      };
      var vars = new TDict { ["inp"] = inp };

      var resp = await TestEnv.Client.PostAsync(query, vars);
      resp.EnsureNoErrors();

      var resArr = resp.GetTopField<object[]>("res");
      var resStr = string.Join("|", resArr).Replace(Environment.NewLine, " ")
        .Replace(" ", String.Empty).Replace("\"", string.Empty);
      var expected = "123|12.34|abc|{name:Parent,value:456,next:{name:Child,value:567,next:null}}|True|";
      Assert.AreEqual(expected, resStr, "Call result mismatch");
    }
    */

  } //class
}
