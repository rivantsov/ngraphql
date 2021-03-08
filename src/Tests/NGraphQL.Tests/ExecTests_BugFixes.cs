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

      TestEnv.LogTestDescr(@"Repro issue #4 - crash with NullRef when using _typename at top level.");

      query = @"
mutation {
  __typename  # causes error
  mutateThing(id: 1, newName: ""newName"") { 
    id 
    name 
  }
}";
      // Bug, old behavior: fails with null ref exc
      // Fixed: now returns error "field _typename not found"
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 result");
      var err0 = resp.Errors[0];
      Assert.IsTrue(err0.Message.Contains(@"Field '__typename' not found"), "Invalid error message"); 
    }

  }
}
