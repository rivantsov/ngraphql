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
      // currently fails with null ref exc
      // the same happens with Query; to fix - remove __typename from top objects
      // also - add sanity check if resolver type is assigned in AssignResolverClassInstance and post more meaningful message
      resp = await ExecuteAsync(query);
      Assert.IsNotNull(resp, "Expected result");

    }

  }

}
