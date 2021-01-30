using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NGraphQL.Server;
using NGraphQL.Utilities;


namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_Out_Lists() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@" Returning plain list of randoms");
      query = @"
query { 
  getThing(id: 1) {
    randoms(count: 5) 
  }
}";
      resp = await ExecuteAsync(query); 
      var randArr = resp.GetValue<int[]>("getThing.randoms");
      Assert.AreEqual(5, randArr.Length, "Expected array of 5 randoms");

      TestEnv.LogTestDescr(@" lists of lists.");
      query = @"
query { 
  res: getIntListRank2()
}";
      resp = await ExecuteAsync(query); // returns [ [3,2,1], [6, 5, 4] ]
      var intArr = resp.GetValue<int[][]>("res");
      Assert.AreEqual(2, intArr.Length, "Expected array of 2 elems");
      Assert.AreEqual(3, intArr[0].Length, "Expected array of 3 elems");

      TestEnv.LogTestDescr(@" list of object types.");
      query = @"
query { 
  res: getThingsList() { name }
}";
      resp = await ExecuteAsync(query);
      var objArr = resp.GetValue<IList<object>>("res");
      Assert.IsNotNull(objArr);

      TestEnv.LogTestDescr(@" list of lists of object types.");
      query = @"
query { 
  res: getThingsListRank2() { name kind }
}";
      resp = await ExecuteAsync(query);
      var objArr2 = resp.GetValue<IList<object>>("res");
      Assert.AreEqual(2, objArr2.Count, "Expected array of 2 elems");
      var childArr = objArr2[0] as IList<object>;
      Assert.AreEqual(2, childArr.Count, "Expected child array of 2 elems");
    }

  } //class
}
