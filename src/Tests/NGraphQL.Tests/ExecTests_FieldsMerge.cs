using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_FieldsMerge() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      query = @"
query { 
  things() { 
    id 
    name 
    name   
    otherThingWrapped {
      otherThingName
      otherThing { name}
    }
    # testing fix for interface entities mapping
    intfThing { id name tag} 
  }
}";
      resp = await ExecuteAsync(query);
      var things = resp.Data["things"];
      Assert.IsNotNull(things, "Expected result");

    }

  }
}



