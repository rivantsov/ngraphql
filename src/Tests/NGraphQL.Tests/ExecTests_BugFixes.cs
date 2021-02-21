using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_BugFixes() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@"Repro issue #169 in VITA.");
      // reported in VITA: https://github.com/rivantsov/vita/issues/169

      var query = @"
query { 
  things() { 
    id 
    name 
    otherThingWrapped {
      otherThingName
      otherThing { name}
    }
  }
}";
      var resp = await ExecuteAsync(query);
      var things = resp.Data["things"];
      Assert.IsNotNull(things, "Expected result");
    }

  }

}
