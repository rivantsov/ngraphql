using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Client;
using Things.GraphQL.Types;

namespace NGraphQL.Tests.HttpTests {
  using TDict = Dictionary<string, object>;

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

    [TestMethod]
    public async Task Test_MapScalar() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" MapScalar test 1; returning Map in field.");


    }

  } //class
}
