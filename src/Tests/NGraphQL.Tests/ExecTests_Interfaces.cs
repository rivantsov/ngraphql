using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;

namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_Misc_Interfaces() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@" simple case, handling Interface return types.");
      query = @"
query { 
        # return type is [NamedObj], NamedObj is interface
  list: getSomeNamedObjects() { __typename, name }
}";
      resp = await ExecuteAsync(query);
      var resList = resp.GetValue<IList>("list");
      Assert.AreEqual(2, resList.Count, "Expected 2 objects");
    }

  } //class
}
